using System.Security.Claims;
using LanguageLearning.API.Common;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Features.Learning;
using LanguageLearning.Domain;

namespace LanguageLearning.API.Features.Learning;

public sealed record QuizAnswersRequest(IReadOnlyDictionary<int, string> Answers);
public sealed record SentencePracticeRequest(
    int LessonStepId,
    string Sentence,
    string GrammarStructure,
    IReadOnlyList<string> Vocabulary);
public sealed record RoadmapProgressRequest(int StepId, int ProgressPercent, int Score);

public static class LearningEndpoints
{
    public static IEndpointRouteBuilder MapLearningEndpoints(this IEndpointRouteBuilder app)
    {
        var lessons = app.MapGroup("/api/lessons").WithTags("Learning");
        lessons.MapGet("/{id:int}", async (
            int id,
            ILearningCatalogService catalog,
            CancellationToken cancellationToken) =>
            await catalog.GetLessonAsync(id, cancellationToken) is { } lesson
                ? Results.Ok(ApiResponse<object>.Ok(lesson))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay bai hoc.")));
        lessons.MapGet("/{id:int}/steps", async (
            int id,
            ILearningCatalogService catalog,
            CancellationToken cancellationToken) =>
            await catalog.GetLessonAsync(id, cancellationToken) is { } lesson
                ? Results.Ok(ApiResponse<object>.Ok(lesson.Steps.OrderBy(x => x.SortOrder)))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay bai hoc.")));
        lessons.MapPost("/steps/{stepId:int}/complete", CompleteStepAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Student, Roles.Admin));
        app.MapPost("/api/lesson-steps/{stepId:int}/complete", CompleteStepAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Student, Roles.Admin))
            .WithTags("Learning");
        lessons.MapPost("/{id:int}/complete", async (
            int id,
            ClaimsPrincipal principal,
            ILearningCatalogService catalog,
            ILearningJourneyService service,
            CancellationToken cancellationToken) =>
        {
            var lesson = await catalog.GetLessonAsync(id, cancellationToken);
            var lastStep = lesson?.Steps.OrderBy(x => x.SortOrder).LastOrDefault();
            if (lastStep is null)
            {
                return Results.NotFound(ApiResponse<object>.Fail("Bai hoc chua co lesson step."));
            }
            var result = await service.CompleteStepAsync(
                principal.GetUserId(),
                lastStep.Id,
                new CompleteStepCommand(100, 100),
                cancellationToken);
            return Results.Ok(ApiResponse<CompleteStepResult?>.Ok(result));
        }).RequireAuthorization();

        app.MapGet("/api/courses/{courseId:int}/roadmap", async (
            int courseId,
            ClaimsPrincipal principal,
            ILearningJourneyService service,
            CancellationToken cancellationToken) =>
            await service.GetCourseRoadmapAsync(
                courseId,
                principal.Identity?.IsAuthenticated == true ? principal.GetUserId() : 0,
                cancellationToken) is { } roadmap
                ? Results.Ok(ApiResponse<RoadmapDto>.Ok(roadmap))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay roadmap.")))
            .WithTags("Roadmap");
        app.MapGet("/api/students/me/roadmap", async (
            ClaimsPrincipal principal,
            ILearningJourneyService service,
            CancellationToken cancellationToken) =>
            await service.GetStudentRoadmapAsync(principal.GetUserId(), cancellationToken) is { } roadmap
                ? Results.Ok(ApiResponse<RoadmapDto>.Ok(roadmap))
                : Results.NotFound(ApiResponse<object>.Fail("Hoc vien chua co khoa hoc.")))
            .RequireAuthorization()
            .WithTags("Roadmap");
        app.MapGet("/api/students/{studentId:int}/roadmap", async (
            int studentId,
            ILearningJourneyService service,
            CancellationToken cancellationToken) =>
            await service.GetStudentRoadmapAsync(studentId, cancellationToken) is { } roadmap
                ? Results.Ok(ApiResponse<RoadmapDto>.Ok(roadmap))
                : Results.NotFound(ApiResponse<object>.Fail("Hoc vien chua co khoa hoc.")))
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Teacher, Roles.Receptionist))
            .WithTags("Roadmap");
        app.MapPost("/api/roadmap/update-progress", async (
            RoadmapProgressRequest request,
            ClaimsPrincipal principal,
            ILearningJourneyService service,
            CancellationToken cancellationToken) =>
            await service.CompleteStepAsync(
                principal.GetUserId(),
                request.StepId,
                new CompleteStepCommand(request.ProgressPercent, request.Score),
                cancellationToken) is { } result
                ? Results.Ok(ApiResponse<CompleteStepResult>.Ok(result))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay lesson step.")))
            .RequireAuthorization()
            .WithTags("Roadmap");

        var vocabulary = app.MapGroup("/api/vocabulary").WithTags("Vocabulary");
        vocabulary.MapGet("/", GetVocabularyAsync).RequireAuthorization();
        vocabulary.MapPost("/", CreateVocabularyAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Teacher));
        vocabulary.MapPut("/{id:int}", UpdateVocabularyAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Teacher));
        app.MapGet("/api/students/me/vocabulary", GetVocabularyAsync)
            .RequireAuthorization()
            .WithTags("Vocabulary");
        app.MapPost("/api/students/me/vocabulary/{id:int}/mark-mastered", async (
            int id,
            ClaimsPrincipal principal,
            ILearningJourneyService service,
            CancellationToken cancellationToken) =>
            await service.MarkVocabularyMasteredAsync(principal.GetUserId(), id, cancellationToken)
                ? Results.Ok(ApiResponse<object>.Ok(new { }, "Da danh dau Mastered."))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay tu vung.")))
            .RequireAuthorization()
            .WithTags("Vocabulary");

        app.MapGet("/api/lessons/{lessonId:int}/flashcards", async (
            int lessonId,
            ILearningJourneyService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<FlashcardDto>>.Ok(
                await service.GetFlashcardsAsync(lessonId, cancellationToken))))
            .WithTags("Flashcards");
        app.MapPost("/api/flashcards/review", async (
            FlashcardReviewCommand command,
            ClaimsPrincipal principal,
            ILearningJourneyService service,
            CancellationToken cancellationToken) =>
        {
            await service.ReviewFlashcardAsync(principal.GetUserId(), command, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(new { }, "Da cap nhat ket qua flashcard."));
        }).RequireAuthorization().WithTags("Flashcards");

        app.MapGet("/api/lessons/{lessonId:int}/grammar", async (
            int lessonId,
            ILearningJourneyService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<GrammarStructureDto>>.Ok(
                await service.GetGrammarAsync(lessonId, cancellationToken))))
            .WithTags("Grammar");
        app.MapPost("/api/grammar-structures", async (
            SaveGrammarCommand command,
            ILearningJourneyService service,
            CancellationToken cancellationToken) =>
        {
            var grammar = await service.CreateGrammarAsync(command, cancellationToken);
            return Results.Created(
                $"/api/lessons/{grammar.LessonId}/grammar",
                ApiResponse<GrammarStructureDto>.Ok(grammar));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Teacher))
            .WithTags("Grammar");

        app.MapGet("/api/quizzes/{lessonId:int}", async (
            int lessonId,
            ILearningCatalogService catalog,
            CancellationToken cancellationToken) =>
            await catalog.GetLessonAsync(lessonId, cancellationToken) is { } lesson
                ? Results.Ok(ApiResponse<object>.Ok(new
                {
                    Id = lessonId,
                    Title = $"{lesson.Title} Quiz",
                    Questions = lesson.Questions.Select(x => new
                    {
                        x.Id,
                        x.QuestionText,
                        x.QuestionType,
                        x.AudioUrl,
                        x.ImageUrl,
                        Options = x.Options.Select(option => new { option.Id, option.OptionText })
                    })
                }))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay quiz.")))
            .WithTags("Quiz");
        app.MapPost("/api/quizzes/{lessonId:int}/submit", SubmitQuizAsync)
            .RequireAuthorization()
            .WithTags("Quiz");
        app.MapGet("/api/students/me/quiz-attempts", async (
            ClaimsPrincipal principal,
            ILearningCatalogService catalog,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<object>.Ok(
                await catalog.GetProgressAsync(principal.GetUserId(), cancellationToken))))
            .RequireAuthorization()
            .WithTags("Quiz");

        app.MapPost("/api/sentence-practice/submit", SubmitSentenceAsync)
            .RequireAuthorization()
            .WithTags("Sentence Practice");
        app.MapGet("/api/sentence-practice/{id:int}/ai-result", async (
            int id,
            ClaimsPrincipal principal,
            ISentencePracticeService service,
            CancellationToken cancellationToken) =>
            await service.GetResultAsync(id, principal.GetUserId(), cancellationToken) is { } result
                ? Results.Ok(ApiResponse<AIScoringResult>.Ok(result))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay ket qua AI.")))
            .RequireAuthorization()
            .WithTags("Sentence Practice");

        return app;
    }

    private static async Task<IResult> CompleteStepAsync(
        int stepId,
        CompleteStepCommand command,
        ClaimsPrincipal principal,
        ILearningJourneyService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return await service.CompleteStepAsync(
                principal.GetUserId(),
                stepId,
                command,
                cancellationToken) is { } result
                ? Results.Ok(ApiResponse<CompleteStepResult>.Ok(result))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay lesson step."));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    private static async Task<IResult> GetVocabularyAsync(
        string? search,
        int? courseId,
        string? topic,
        string? level,
        string? status,
        ClaimsPrincipal principal,
        ILearningJourneyService service,
        CancellationToken cancellationToken) =>
        Results.Ok(ApiResponse<IReadOnlyList<VocabularyItemDto>>.Ok(
            await service.GetVocabularyAsync(
                principal.GetUserId(),
                search,
                courseId,
                topic,
                level,
                status,
                cancellationToken)));

    private static async Task<IResult> CreateVocabularyAsync(
        SaveVocabularyCommand command,
        ILearningJourneyService service,
        CancellationToken cancellationToken)
    {
        if (command.LessonId <= 0 || string.IsNullOrWhiteSpace(command.Word))
        {
            return Results.BadRequest(ApiResponse<object>.Fail("LessonId va Word la bat buoc."));
        }
        var item = await service.CreateVocabularyAsync(command, cancellationToken);
        return Results.Created($"/api/vocabulary/{item.Id}", ApiResponse<VocabularyItemDto>.Ok(item));
    }

    private static async Task<IResult> UpdateVocabularyAsync(
        int id,
        SaveVocabularyCommand command,
        ILearningJourneyService service,
        CancellationToken cancellationToken) =>
        await service.UpdateVocabularyAsync(id, command, cancellationToken) is { } item
            ? Results.Ok(ApiResponse<VocabularyItemDto>.Ok(item))
            : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay tu vung."));

    private static async Task<IResult> SubmitQuizAsync(
        int lessonId,
        QuizAnswersRequest request,
        ClaimsPrincipal principal,
        ILearningCatalogService catalog,
        CancellationToken cancellationToken)
    {
        var result = await catalog.SubmitQuizAsync(
            principal.GetUserId(),
            new QuizSubmission(lessonId, request.Answers),
            cancellationToken);
        return Results.Ok(ApiResponse<QuizResult>.Ok(result, "Da cham diem."));
    }

    private static async Task<IResult> SubmitSentenceAsync(
        SentencePracticeRequest request,
        ClaimsPrincipal principal,
        ISentencePracticeService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Sentence))
        {
            return Results.BadRequest(ApiResponse<object>.Fail("Sentence la bat buoc."));
        }
        var result = await service.SubmitAsync(
            new SentenceScoringRequest(
                principal.GetUserId(),
                request.LessonStepId,
                request.Sentence,
                request.GrammarStructure,
                request.Vocabulary),
            cancellationToken);
        return Results.Ok(ApiResponse<AIScoringResult>.Ok(result, "AI da cham diem."));
    }
}

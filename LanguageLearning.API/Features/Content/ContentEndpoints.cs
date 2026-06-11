using LanguageLearning.API.Common;
using LanguageLearning.Application.Features.Content;
using LanguageLearning.Domain;

namespace LanguageLearning.API.Features.Content;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/content")
            .WithTags("Content Authoring")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Teacher));

        group.MapPost("/lesson-steps", async (
            SaveLessonStepCommand command,
            IContentManagementService service,
            CancellationToken cancellationToken) =>
        {
            var error = ValidateStep(command);
            if (error is not null)
            {
                return Results.BadRequest(ApiResponse<object>.Fail(error));
            }
            var step = await service.CreateLessonStepAsync(command, cancellationToken);
            return Results.Created($"/api/lesson-steps/{step.Id}", ApiResponse<LessonStepDto>.Ok(step));
        });
        group.MapPut("/lesson-steps/{id:int}", async (
            int id,
            SaveLessonStepCommand command,
            IContentManagementService service,
            CancellationToken cancellationToken) =>
            await service.UpdateLessonStepAsync(id, command, cancellationToken) is { } step
                ? Results.Ok(ApiResponse<LessonStepDto>.Ok(step))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay lesson step.")));
        group.MapPost("/videos", async (
            SaveLessonVideoCommand command,
            IContentManagementService service,
            CancellationToken cancellationToken) =>
        {
            if (command.LessonStepId <= 0 || string.IsNullOrWhiteSpace(command.VideoUrl))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("LessonStepId va VideoUrl la bat buoc."));
            }
            var id = await service.CreateVideoAsync(command, cancellationToken);
            return Results.Created($"/api/videos/{id}", ApiResponse<object>.Ok(new { Id = id }));
        });
        group.MapPost("/flashcards", async (
            SaveFlashcardCommand command,
            IContentManagementService service,
            CancellationToken cancellationToken) =>
        {
            if (command.VocabularyId <= 0 || string.IsNullOrWhiteSpace(command.FrontText))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("VocabularyId va FrontText la bat buoc."));
            }
            var id = await service.CreateFlashcardAsync(command, cancellationToken);
            return Results.Created($"/api/flashcards/{id}", ApiResponse<object>.Ok(new { Id = id }));
        });
        group.MapPost("/questions", CreateQuestionAsync);
        group.MapPut("/questions/{id:int}", async (
            int id,
            SaveQuestionCommand command,
            IContentManagementService service,
            CancellationToken cancellationToken) =>
            await service.UpdateQuestionAsync(id, command, cancellationToken)
                ? Results.Ok(ApiResponse<object>.Ok(new { }, "Da cap nhat question."))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay question.")));
        group.MapDelete("/questions/{id:int}", async (
            int id,
            IContentManagementService service,
            CancellationToken cancellationToken) =>
            await service.DeleteQuestionAsync(id, cancellationToken)
                ? Results.Ok(ApiResponse<object>.Ok(new { }, "Da xoa question."))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay question.")));

        return app;
    }

    private static async Task<IResult> CreateQuestionAsync(
        SaveQuestionCommand command,
        IContentManagementService service,
        CancellationToken cancellationToken)
    {
        if (command.LessonId <= 0
            || string.IsNullOrWhiteSpace(command.QuestionText)
            || string.IsNullOrWhiteSpace(command.CorrectAnswer))
        {
            return Results.BadRequest(ApiResponse<object>.Fail(
                "LessonId, QuestionText va CorrectAnswer la bat buoc."));
        }
        var id = await service.CreateQuestionAsync(command, cancellationToken);
        return Results.Created($"/api/questions/{id}", ApiResponse<object>.Ok(new { Id = id }));
    }

    private static string? ValidateStep(SaveLessonStepCommand command)
    {
        if (command.LessonId <= 0 || string.IsNullOrWhiteSpace(command.Title))
        {
            return "LessonId va Title la bat buoc.";
        }
        if (command.MinScoreToPass is < 0 or > 100)
        {
            return "MinScoreToPass phai tu 0 den 100.";
        }
        return null;
    }
}

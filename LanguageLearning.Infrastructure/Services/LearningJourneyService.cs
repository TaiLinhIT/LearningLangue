using LanguageLearning.Application.Features.Learning;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Services;

public sealed class LearningJourneyService(LanguageLearningDbContext db) : ILearningJourneyService
{
    public async Task<RoadmapDto?> GetCourseRoadmapAsync(
        int courseId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var course = await db.Courses
            .Include(x => x.Units.OrderBy(unit => unit.SortOrder))
            .ThenInclude(x => x.Lessons.OrderBy(lesson => lesson.SortOrder))
            .ThenInclude(x => x.Steps.OrderBy(step => step.SortOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken);
        if (course is null)
        {
            return null;
        }

        var lessonIds = course.Units.SelectMany(x => x.Lessons).Select(x => x.Id).ToList();
        var stepIds = course.Units.SelectMany(x => x.Lessons).SelectMany(x => x.Steps).Select(x => x.Id).ToList();
        var lessonProgress = await db.UserLessonProgress
            .AsNoTracking()
            .Where(x => x.UserId == userId && lessonIds.Contains(x.LessonId))
            .ToDictionaryAsync(x => x.LessonId, cancellationToken);
        var stepProgress = await db.StudentStepProgress
            .AsNoTracking()
            .Where(x => x.UserId == userId && stepIds.Contains(x.LessonStepId))
            .ToDictionaryAsync(x => x.LessonStepId, cancellationToken);

        var orderedLessons = course.Units
            .OrderBy(x => x.SortOrder)
            .SelectMany(x => x.Lessons.OrderBy(lesson => lesson.SortOrder))
            .ToList();
        var firstIncompleteLessonIndex = orderedLessons.FindIndex(
            lesson => !lessonProgress.TryGetValue(lesson.Id, out var progress)
                || progress.Status != ProgressStatuses.Completed);
        if (firstIncompleteLessonIndex < 0)
        {
            firstIncompleteLessonIndex = orderedLessons.Count;
        }

        var units = course.Units.OrderBy(x => x.SortOrder).Select(unit =>
            new RoadmapUnitDto(
                unit.Id,
                unit.Title,
                unit.SortOrder,
                unit.Lessons.OrderBy(x => x.SortOrder).Select(lesson =>
                {
                    var lessonIndex = orderedLessons.FindIndex(x => x.Id == lesson.Id);
                    lessonProgress.TryGetValue(lesson.Id, out var progress);
                    var lessonLocked = lessonIndex > firstIncompleteLessonIndex;
                    var orderedSteps = lesson.Steps.OrderBy(x => x.SortOrder).ToList();
                    var firstIncompleteStepIndex = orderedSteps.FindIndex(
                        step => !stepProgress.TryGetValue(step.Id, out var current)
                            || current.Status != ProgressStatuses.Completed);
                    if (firstIncompleteStepIndex < 0)
                    {
                        firstIncompleteStepIndex = orderedSteps.Count;
                    }

                    var steps = orderedSteps.Select((step, index) =>
                    {
                        stepProgress.TryGetValue(step.Id, out var current);
                        return new RoadmapStepDto(
                            step.Id,
                            step.StepType,
                            step.Title,
                            step.SortOrder,
                            step.MinScoreToPass,
                            current?.Status ?? ProgressStatuses.NotStarted,
                            current?.Score ?? 0,
                            current?.ProgressPercent ?? 0,
                            lessonLocked || index > firstIncompleteStepIndex);
                    }).ToList();

                    return new RoadmapLessonDto(
                        lesson.Id,
                        lesson.Title,
                        lesson.SortOrder,
                        lesson.XPReward,
                        progress?.Status ?? ProgressStatuses.NotStarted,
                        lessonLocked,
                        steps);
                }).ToList())).ToList();

        var completed = lessonProgress.Values.Count(x => x.Status == ProgressStatuses.Completed);
        var total = orderedLessons.Count;
        return new RoadmapDto(
            course.Id,
            course.Title,
            total == 0 ? 0 : (int)Math.Round(completed * 100m / total),
            completed,
            total,
            units);
    }

    public async Task<RoadmapDto?> GetStudentRoadmapAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var courseId = await db.CourseEnrollments
            .Where(x => x.StudentId == userId && x.Status == "Active")
            .OrderBy(x => x.EnrolledAt)
            .Select(x => (int?)x.CourseId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await db.UserCourses
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.StartedAt)
                .Select(x => (int?)x.CourseId)
                .FirstOrDefaultAsync(cancellationToken);
        return courseId.HasValue
            ? await GetCourseRoadmapAsync(courseId.Value, userId, cancellationToken)
            : null;
    }

    public async Task<CompleteStepResult?> CompleteStepAsync(
        int userId,
        int stepId,
        CompleteStepCommand command,
        CancellationToken cancellationToken = default)
    {
        var step = await db.LessonSteps
            .Include(x => x.Lesson)
            .ThenInclude(x => x!.Unit)
            .FirstOrDefaultAsync(x => x.Id == stepId, cancellationToken);
        if (step?.Lesson is null)
        {
            return null;
        }

        var orderedSteps = await db.LessonSteps
            .Where(x => x.LessonId == step.LessonId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        var stepIndex = orderedSteps.FindIndex(x => x.Id == stepId);
        if (stepIndex > 0)
        {
            var previousStepId = orderedSteps[stepIndex - 1].Id;
            var previousCompleted = await db.StudentStepProgress.AnyAsync(
                x => x.UserId == userId
                    && x.LessonStepId == previousStepId
                    && x.Status == ProgressStatuses.Completed,
                cancellationToken);
            if (!previousCompleted)
            {
                throw new InvalidOperationException("Buoc truoc chua hoan thanh.");
            }
        }

        var passed = command.ProgressPercent >= 100
            && (step.MinScoreToPass == 0 || command.Score >= step.MinScoreToPass);
        var progress = await db.StudentStepProgress.FirstOrDefaultAsync(
            x => x.UserId == userId && x.LessonStepId == stepId,
            cancellationToken);
        if (progress is null)
        {
            progress = new StudentStepProgress
            {
                UserId = userId,
                LessonStepId = stepId,
                StartedAt = DateTime.UtcNow
            };
            db.StudentStepProgress.Add(progress);
        }

        progress.Score = Math.Max(progress.Score, command.Score);
        progress.ProgressPercent = Math.Max(progress.ProgressPercent, command.ProgressPercent);
        progress.Status = passed ? ProgressStatuses.Completed : ProgressStatuses.InProgress;
        progress.CompletedAt = passed ? DateTime.UtcNow : null;

        if (passed && step.StepType == LessonStepTypes.Video)
        {
            var vocabularyIds = await db.Vocabulary
                .Where(x => x.LessonId == step.LessonId)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            var existing = await db.StudentVocabulary
                .Where(x => x.StudentId == userId && vocabularyIds.Contains(x.VocabularyId))
                .Select(x => x.VocabularyId)
                .ToListAsync(cancellationToken);
            db.StudentVocabulary.AddRange(vocabularyIds.Except(existing).Select(id => new StudentVocabulary
            {
                StudentId = userId,
                VocabularyId = id
            }));
        }

        var lessonCompleted = false;
        var earnedXp = 0;
        int? unlockedLessonId = null;
        if (passed && stepIndex == orderedSteps.Count - 1)
        {
            lessonCompleted = true;
            earnedXp = step.Lesson.XPReward;
            var lessonProgress = await db.UserLessonProgress.FirstOrDefaultAsync(
                x => x.UserId == userId && x.LessonId == step.LessonId,
                cancellationToken);
            if (lessonProgress is null)
            {
                lessonProgress = new UserLessonProgress { UserId = userId, LessonId = step.LessonId };
                db.UserLessonProgress.Add(lessonProgress);
            }

            if (lessonProgress.Status != ProgressStatuses.Completed)
            {
                lessonProgress.XP += earnedXp;
            }

            lessonProgress.Status = ProgressStatuses.Completed;
            lessonProgress.Score = Math.Max(lessonProgress.Score, command.Score);
            lessonProgress.CompletedAt = DateTime.UtcNow;
            unlockedLessonId = await FindNextLessonIdAsync(step.Lesson, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new CompleteStepResult(
            stepId,
            progress.Status,
            progress.Score,
            progress.ProgressPercent,
            passed && stepIndex + 1 < orderedSteps.Count ? orderedSteps[stepIndex + 1].Id : null,
            lessonCompleted,
            earnedXp,
            unlockedLessonId);
    }

    public async Task<IReadOnlyList<VocabularyItemDto>> GetVocabularyAsync(
        int userId,
        string? search,
        int? courseId,
        string? topic,
        string? level,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = db.Vocabulary
            .Include(x => x.Lesson)
            .ThenInclude(x => x!.Unit)
            .AsNoTracking()
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Word.Contains(search) || x.Meaning.Contains(search));
        }
        if (courseId.HasValue)
        {
            query = query.Where(x => x.Lesson!.Unit!.CourseId == courseId.Value);
        }
        if (!string.IsNullOrWhiteSpace(topic))
        {
            query = query.Where(x => x.Topic == topic);
        }
        if (!string.IsNullOrWhiteSpace(level))
        {
            query = query.Where(x => x.Level == level);
        }

        var words = await query.OrderBy(x => x.Word).ToListAsync(cancellationToken);
        var ids = words.Select(x => x.Id).ToList();
        var studentWords = await db.StudentVocabulary
            .AsNoTracking()
            .Where(x => x.StudentId == userId && ids.Contains(x.VocabularyId))
            .ToDictionaryAsync(x => x.VocabularyId, cancellationToken);
        return words
            .Select(word =>
            {
                studentWords.TryGetValue(word.Id, out var studentWord);
                return MapVocabulary(word, studentWord);
            })
            .Where(x => string.IsNullOrWhiteSpace(status) || x.Status == status)
            .ToList();
    }

    public async Task<VocabularyItemDto> CreateVocabularyAsync(
        SaveVocabularyCommand command,
        CancellationToken cancellationToken = default)
    {
        var word = Apply(new Vocabulary(), command);
        db.Vocabulary.Add(word);
        await db.SaveChangesAsync(cancellationToken);
        return MapVocabulary(word, null);
    }

    public async Task<VocabularyItemDto?> UpdateVocabularyAsync(
        int id,
        SaveVocabularyCommand command,
        CancellationToken cancellationToken = default)
    {
        var word = await db.Vocabulary.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (word is null)
        {
            return null;
        }

        Apply(word, command);
        await db.SaveChangesAsync(cancellationToken);
        return MapVocabulary(word, null);
    }

    public async Task<bool> MarkVocabularyMasteredAsync(
        int userId,
        int vocabularyId,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Vocabulary.AnyAsync(x => x.Id == vocabularyId, cancellationToken))
        {
            return false;
        }

        var row = await db.StudentVocabulary.FirstOrDefaultAsync(
            x => x.StudentId == userId && x.VocabularyId == vocabularyId,
            cancellationToken);
        if (row is null)
        {
            row = new StudentVocabulary { StudentId = userId, VocabularyId = vocabularyId };
            db.StudentVocabulary.Add(row);
        }

        row.Status = "Mastered";
        row.ReviewCount++;
        row.LastReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<FlashcardDto>> GetFlashcardsAsync(
        int lessonId,
        CancellationToken cancellationToken = default) =>
        await db.Flashcards
            .Where(x => x.Vocabulary!.LessonId == lessonId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new FlashcardDto(
                x.Id,
                x.VocabularyId,
                x.FrontText,
                x.BackText,
                x.ImageUrl,
                x.AudioUrl,
                x.Vocabulary!.Pronunciation,
                x.Vocabulary.ExampleSentence,
                x.SortOrder))
            .ToListAsync(cancellationToken);

    public async Task ReviewFlashcardAsync(
        int userId,
        FlashcardReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        var vocabularyId = await db.Flashcards
            .Where(x => x.Id == command.FlashcardId)
            .Select(x => x.VocabularyId)
            .FirstAsync(cancellationToken);
        var row = await db.StudentVocabulary.FirstOrDefaultAsync(
            x => x.StudentId == userId && x.VocabularyId == vocabularyId,
            cancellationToken);
        if (row is null)
        {
            row = new StudentVocabulary { StudentId = userId, VocabularyId = vocabularyId };
            db.StudentVocabulary.Add(row);
        }

        row.Status = command.Mastered ? "Mastered" : "Learning";
        row.ReviewCount++;
        row.LastReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GrammarStructureDto>> GetGrammarAsync(
        int lessonId,
        CancellationToken cancellationToken = default) =>
        await db.GrammarStructures.AsNoTracking()
            .Where(x => x.LessonId == lessonId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new GrammarStructureDto(
                x.Id,
                x.LessonId,
                x.Title,
                x.StructurePattern,
                x.Explanation,
                x.ExampleSentence,
                x.VietnameseMeaning,
                x.SortOrder))
            .ToListAsync(cancellationToken);

    public async Task<GrammarStructureDto> CreateGrammarAsync(
        SaveGrammarCommand command,
        CancellationToken cancellationToken = default)
    {
        var grammar = new GrammarStructure
        {
            LessonId = command.LessonId,
            Title = command.Title.Trim(),
            StructurePattern = command.StructurePattern.Trim(),
            Explanation = command.Explanation.Trim(),
            ExampleSentence = command.ExampleSentence.Trim(),
            VietnameseMeaning = command.VietnameseMeaning.Trim(),
            SortOrder = command.SortOrder
        };
        db.GrammarStructures.Add(grammar);
        await db.SaveChangesAsync(cancellationToken);
        return new GrammarStructureDto(
            grammar.Id,
            grammar.LessonId,
            grammar.Title,
            grammar.StructurePattern,
            grammar.Explanation,
            grammar.ExampleSentence,
            grammar.VietnameseMeaning,
            grammar.SortOrder);
    }

    private async Task<int?> FindNextLessonIdAsync(Lesson lesson, CancellationToken cancellationToken)
    {
        var courseId = lesson.Unit!.CourseId;
        var ordered = await db.Lessons
            .Where(x => x.Unit!.CourseId == courseId)
            .OrderBy(x => x.Unit!.SortOrder)
            .ThenBy(x => x.SortOrder)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var index = ordered.IndexOf(lesson.Id);
        return index >= 0 && index + 1 < ordered.Count ? ordered[index + 1] : null;
    }

    private static Vocabulary Apply(Vocabulary word, SaveVocabularyCommand command)
    {
        word.LessonId = command.LessonId;
        word.Word = command.Word.Trim();
        word.Meaning = command.Meaning.Trim();
        word.Pronunciation = command.Pronunciation?.Trim();
        word.ExampleSentence = command.ExampleSentence?.Trim();
        word.AudioUrl = command.AudioUrl?.Trim();
        word.ImageUrl = command.ImageUrl?.Trim();
        word.Topic = command.Topic?.Trim() ?? string.Empty;
        word.Level = command.Level?.Trim().ToUpperInvariant() ?? "A1";
        return word;
    }

    private static VocabularyItemDto MapVocabulary(Vocabulary word, StudentVocabulary? studentWord) =>
        new(
            word.Id,
            word.LessonId,
            word.Word,
            word.Meaning,
            word.Pronunciation,
            word.ExampleSentence,
            word.AudioUrl,
            word.ImageUrl,
            word.Topic,
            word.Level,
            studentWord?.Status ?? "New",
            studentWord?.ReviewCount ?? 0);
}

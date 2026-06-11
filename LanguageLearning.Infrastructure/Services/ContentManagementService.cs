using LanguageLearning.Application.Features.Content;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Services;

public sealed class ContentManagementService(LanguageLearningDbContext db) : IContentManagementService
{
    public async Task<LessonStepDto> CreateLessonStepAsync(
        SaveLessonStepCommand command,
        CancellationToken cancellationToken = default)
    {
        var step = Apply(new LessonStep(), command);
        db.LessonSteps.Add(step);
        await db.SaveChangesAsync(cancellationToken);
        return Map(step);
    }

    public async Task<LessonStepDto?> UpdateLessonStepAsync(
        int id,
        SaveLessonStepCommand command,
        CancellationToken cancellationToken = default)
    {
        var step = await db.LessonSteps.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (step is null)
        {
            return null;
        }

        Apply(step, command);
        await db.SaveChangesAsync(cancellationToken);
        return Map(step);
    }

    public async Task<int> CreateVideoAsync(
        SaveLessonVideoCommand command,
        CancellationToken cancellationToken = default)
    {
        var video = new LessonVideo
        {
            LessonStepId = command.LessonStepId,
            Title = command.Title.Trim(),
            VideoUrl = command.VideoUrl.Trim(),
            SubtitleUrl = command.SubtitleUrl?.Trim(),
            DurationSeconds = command.DurationSeconds,
            RequiredWatchPercent = command.RequiredWatchPercent,
            TranscriptText = command.TranscriptText?.Trim()
        };
        db.LessonVideos.Add(video);
        await db.SaveChangesAsync(cancellationToken);
        return video.Id;
    }

    public async Task<int> CreateFlashcardAsync(
        SaveFlashcardCommand command,
        CancellationToken cancellationToken = default)
    {
        var card = new Flashcard
        {
            VocabularyId = command.VocabularyId,
            FrontText = command.FrontText.Trim(),
            BackText = command.BackText.Trim(),
            ImageUrl = command.ImageUrl?.Trim(),
            AudioUrl = command.AudioUrl?.Trim(),
            SortOrder = command.SortOrder
        };
        db.Flashcards.Add(card);
        await db.SaveChangesAsync(cancellationToken);
        return card.Id;
    }

    public async Task<int> CreateQuestionAsync(
        SaveQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        var question = Apply(new Question(), command);
        db.Questions.Add(question);
        await db.SaveChangesAsync(cancellationToken);
        return question.Id;
    }

    public async Task<bool> UpdateQuestionAsync(
        int id,
        SaveQuestionCommand command,
        CancellationToken cancellationToken = default)
    {
        var question = await db.Questions
            .Include(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (question is null)
        {
            return false;
        }

        db.QuestionOptions.RemoveRange(question.Options);
        Apply(question, command);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteQuestionAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var question = await db.Questions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (question is null)
        {
            return false;
        }

        db.Questions.Remove(question);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static LessonStep Apply(LessonStep step, SaveLessonStepCommand command)
    {
        step.LessonId = command.LessonId;
        step.StepType = command.StepType;
        step.Title = command.Title.Trim();
        step.Description = command.Description.Trim();
        step.SortOrder = command.SortOrder;
        step.IsRequired = command.IsRequired;
        step.MinScoreToPass = command.MinScoreToPass;
        step.ContentUrl = command.ContentUrl?.Trim();
        return step;
    }

    private static Question Apply(Question question, SaveQuestionCommand command)
    {
        question.LessonId = command.LessonId;
        question.QuestionText = command.QuestionText.Trim();
        question.QuestionType = command.QuestionType.Trim();
        question.CorrectAnswer = command.CorrectAnswer.Trim();
        question.Explanation = command.Explanation?.Trim();
        question.AudioUrl = command.AudioUrl?.Trim();
        question.ImageUrl = command.ImageUrl?.Trim();
        question.Options = command.Options.Select(option => new QuestionOption
        {
            OptionText = option.OptionText.Trim(),
            IsCorrect = option.IsCorrect
        }).ToList();
        return question;
    }

    private static LessonStepDto Map(LessonStep step) =>
        new(
            step.Id,
            step.LessonId,
            step.StepType,
            step.Title,
            step.Description,
            step.SortOrder,
            step.IsRequired,
            step.MinScoreToPass,
            step.ContentUrl);
}

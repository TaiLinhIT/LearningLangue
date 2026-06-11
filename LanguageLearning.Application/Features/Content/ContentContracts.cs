namespace LanguageLearning.Application.Features.Content;

public sealed record SaveLessonStepCommand(
    int LessonId,
    string StepType,
    string Title,
    string Description,
    int SortOrder,
    bool IsRequired,
    int MinScoreToPass,
    string? ContentUrl);

public sealed record LessonStepDto(
    int Id,
    int LessonId,
    string StepType,
    string Title,
    string Description,
    int SortOrder,
    bool IsRequired,
    int MinScoreToPass,
    string? ContentUrl);

public sealed record SaveLessonVideoCommand(
    int LessonStepId,
    string Title,
    string VideoUrl,
    string? SubtitleUrl,
    int DurationSeconds,
    int RequiredWatchPercent,
    string? TranscriptText);

public sealed record SaveFlashcardCommand(
    int VocabularyId,
    string FrontText,
    string BackText,
    string? ImageUrl,
    string? AudioUrl,
    int SortOrder);

public sealed record QuestionOptionCommand(string OptionText, bool IsCorrect);

public sealed record SaveQuestionCommand(
    int LessonId,
    string QuestionText,
    string QuestionType,
    string CorrectAnswer,
    string? Explanation,
    string? AudioUrl,
    string? ImageUrl,
    IReadOnlyList<QuestionOptionCommand> Options);

public interface IContentManagementService
{
    Task<LessonStepDto> CreateLessonStepAsync(SaveLessonStepCommand command, CancellationToken cancellationToken = default);
    Task<LessonStepDto?> UpdateLessonStepAsync(int id, SaveLessonStepCommand command, CancellationToken cancellationToken = default);
    Task<int> CreateVideoAsync(SaveLessonVideoCommand command, CancellationToken cancellationToken = default);
    Task<int> CreateFlashcardAsync(SaveFlashcardCommand command, CancellationToken cancellationToken = default);
    Task<int> CreateQuestionAsync(SaveQuestionCommand command, CancellationToken cancellationToken = default);
    Task<bool> UpdateQuestionAsync(int id, SaveQuestionCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteQuestionAsync(int id, CancellationToken cancellationToken = default);
}

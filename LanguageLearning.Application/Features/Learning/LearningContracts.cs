namespace LanguageLearning.Application.Features.Learning;

public sealed record RoadmapStepDto(
    int Id,
    string Type,
    string Title,
    int SortOrder,
    int MinScoreToPass,
    string Status,
    int Score,
    int ProgressPercent,
    bool IsLocked);

public sealed record RoadmapLessonDto(
    int Id,
    string Title,
    int SortOrder,
    int XPReward,
    string Status,
    bool IsLocked,
    IReadOnlyList<RoadmapStepDto> Steps);

public sealed record RoadmapUnitDto(
    int Id,
    string Title,
    int SortOrder,
    IReadOnlyList<RoadmapLessonDto> Lessons);

public sealed record RoadmapDto(
    int CourseId,
    string CourseTitle,
    int ProgressPercent,
    int CompletedLessons,
    int TotalLessons,
    IReadOnlyList<RoadmapUnitDto> Units);

public sealed record CompleteStepCommand(int ProgressPercent, int Score);

public sealed record CompleteStepResult(
    int StepId,
    string Status,
    int Score,
    int ProgressPercent,
    int? UnlockedStepId,
    bool LessonCompleted,
    int EarnedXP,
    int? UnlockedLessonId);

public sealed record VocabularyItemDto(
    int Id,
    int LessonId,
    string Word,
    string Meaning,
    string? Pronunciation,
    string? ExampleSentence,
    string? AudioUrl,
    string? ImageUrl,
    string Topic,
    string Level,
    string Status,
    int ReviewCount);

public sealed record SaveVocabularyCommand(
    int LessonId,
    string Word,
    string Meaning,
    string? Pronunciation,
    string? ExampleSentence,
    string? AudioUrl,
    string? ImageUrl,
    string? Topic,
    string? Level);

public sealed record FlashcardDto(
    int Id,
    int VocabularyId,
    string FrontText,
    string BackText,
    string? ImageUrl,
    string? AudioUrl,
    string? Pronunciation,
    string? ExampleSentence,
    int SortOrder);

public sealed record FlashcardReviewCommand(int FlashcardId, bool Mastered);

public sealed record GrammarStructureDto(
    int Id,
    int LessonId,
    string Title,
    string StructurePattern,
    string Explanation,
    string ExampleSentence,
    string VietnameseMeaning,
    int SortOrder);

public sealed record SaveGrammarCommand(
    int LessonId,
    string Title,
    string StructurePattern,
    string Explanation,
    string ExampleSentence,
    string VietnameseMeaning,
    int SortOrder);

public interface ILearningJourneyService
{
    Task<RoadmapDto?> GetCourseRoadmapAsync(int courseId, int userId, CancellationToken cancellationToken = default);
    Task<RoadmapDto?> GetStudentRoadmapAsync(int userId, CancellationToken cancellationToken = default);
    Task<CompleteStepResult?> CompleteStepAsync(int userId, int stepId, CompleteStepCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VocabularyItemDto>> GetVocabularyAsync(
        int userId,
        string? search,
        int? courseId,
        string? topic,
        string? level,
        string? status,
        CancellationToken cancellationToken = default);
    Task<VocabularyItemDto> CreateVocabularyAsync(SaveVocabularyCommand command, CancellationToken cancellationToken = default);
    Task<VocabularyItemDto?> UpdateVocabularyAsync(int id, SaveVocabularyCommand command, CancellationToken cancellationToken = default);
    Task<bool> MarkVocabularyMasteredAsync(int userId, int vocabularyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FlashcardDto>> GetFlashcardsAsync(int lessonId, CancellationToken cancellationToken = default);
    Task ReviewFlashcardAsync(int userId, FlashcardReviewCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrammarStructureDto>> GetGrammarAsync(int lessonId, CancellationToken cancellationToken = default);
    Task<GrammarStructureDto> CreateGrammarAsync(SaveGrammarCommand command, CancellationToken cancellationToken = default);
}

namespace LanguageLearning.Application.Features.Engagement;

public sealed record RankingDto(
    int Rank,
    int StudentId,
    string StudentName,
    int XP,
    int QuizScore,
    int AIPracticeScore,
    int CompletionPoint,
    int TotalPoint,
    int Streak);

public sealed record RewardDto(
    int Id,
    string Title,
    string Description,
    string RewardType,
    int? RequiredRank,
    int? RequiredPoint,
    int? CourseId,
    int? ClassId,
    bool IsActive,
    string? StudentStatus);

public sealed record SaveRewardCommand(
    string Title,
    string Description,
    string RewardType,
    int? RequiredRank,
    int? RequiredPoint,
    int? CourseId,
    int? ClassId,
    bool IsActive);

public sealed record NotificationDto(
    int Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    DateTime CreatedAt);

public sealed record IpaSoundDto(
    int Id,
    string Symbol,
    string SoundType,
    string ExampleWord,
    string? AudioUrl,
    string? VideoUrl,
    string VietnameseGuide,
    string? ComparisonNote,
    int SortOrder);

public sealed record IpaExerciseDto(
    int Id,
    int LessonId,
    string QuestionText,
    string CorrectAnswer,
    string? AudioUrl,
    string Explanation);

public sealed record IpaLessonDto(
    int Id,
    string Title,
    string Description,
    string SoundType,
    int SortOrder,
    int ExerciseCount);

public sealed record IpaSubmissionResult(bool Correct, string Explanation);

public sealed record ReportDto(
    int Id,
    int ReporterId,
    string ReporterName,
    string TargetType,
    int TargetId,
    string Reason,
    string Status,
    DateTime CreatedAt);

public interface IEngagementService
{
    Task<IReadOnlyList<RankingDto>> GetClassRankingAsync(int classId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RankingDto>> GetCenterRankingAsync(CancellationToken cancellationToken = default);
    Task<RankingDto?> GetStudentRankingAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RewardDto>> GetRewardsAsync(int? userId, CancellationToken cancellationToken = default);
    Task<RewardDto> CreateRewardAsync(SaveRewardCommand command, CancellationToken cancellationToken = default);
    Task<int> CalculateRewardsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> MarkNotificationReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IpaSoundDto>> GetIpaSoundsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IpaLessonDto>> GetIpaLessonsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IpaExerciseDto>> GetIpaExercisesAsync(CancellationToken cancellationToken = default);
    Task<IpaSubmissionResult?> SubmitIpaExerciseAsync(int exerciseId, string answer, CancellationToken cancellationToken = default);
    Task<ReportDto> CreateReportAsync(int userId, string targetType, int targetId, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportDto>> GetReportsAsync(CancellationToken cancellationToken = default);
}

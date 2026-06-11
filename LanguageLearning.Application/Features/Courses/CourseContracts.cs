namespace LanguageLearning.Application.Features.Courses;

public sealed record CourseSummaryDto(
    int Id,
    int LanguageId,
    string LanguageName,
    string Title,
    string Description,
    string Level,
    string? ThumbnailUrl,
    bool IsPublished,
    int UnitCount,
    int LessonCount);

public sealed record SaveCourseCommand(
    int LanguageId,
    string Title,
    string Description,
    string Level,
    string? ThumbnailUrl,
    bool IsPublished);

public sealed record SaveUnitCommand(int CourseId, string Title, int SortOrder);

public sealed record SaveLessonCommand(
    int UnitId,
    string Title,
    string Description,
    string LessonType,
    int XPReward,
    int SortOrder,
    bool IsLocked);

public interface ICourseManagementService
{
    Task<IReadOnlyList<CourseSummaryDto>> GetAsync(bool includeDrafts, CancellationToken cancellationToken = default);
    Task<CourseSummaryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CourseSummaryDto> CreateAsync(SaveCourseCommand command, CancellationToken cancellationToken = default);
    Task<CourseSummaryDto?> UpdateAsync(int id, SaveCourseCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> PublishAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateUnitAsync(SaveUnitCommand command, CancellationToken cancellationToken = default);
    Task<int> CreateLessonAsync(SaveLessonCommand command, CancellationToken cancellationToken = default);
}

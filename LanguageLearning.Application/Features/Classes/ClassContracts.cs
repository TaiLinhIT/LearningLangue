namespace LanguageLearning.Application.Features.Classes;

public sealed record ClassDto(
    int Id,
    string Name,
    string Level,
    int? TeacherId,
    string? TeacherName,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive,
    int StudentCount);

public sealed record SaveClassCommand(
    string Name,
    string Level,
    int? TeacherId,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive);

public sealed record AssignStudentCommand(int StudentId, int? CourseId);

public sealed record ClassStudentDto(
    int StudentId,
    string FullName,
    string Email,
    string Level,
    DateTime JoinedAt);

public sealed record CommentDto(
    int Id,
    int ClassId,
    int UserId,
    string AuthorName,
    string AuthorRole,
    string Content,
    bool IsPinned,
    DateTime CreatedAt,
    IReadOnlyList<CommentReplyDto> Replies);

public sealed record CommentReplyDto(
    int Id,
    int UserId,
    string AuthorName,
    string Content,
    DateTime CreatedAt);

public interface IClassroomService
{
    Task<IReadOnlyList<ClassDto>> GetClassesAsync(int? userId, string? role, CancellationToken cancellationToken = default);
    Task<ClassDto?> GetClassAsync(int id, CancellationToken cancellationToken = default);
    Task<ClassDto> CreateAsync(SaveClassCommand command, CancellationToken cancellationToken = default);
    Task<ClassDto?> UpdateAsync(int id, SaveClassCommand command, CancellationToken cancellationToken = default);
    Task<bool> AssignStudentAsync(int classId, AssignStudentCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassStudentDto>> GetStudentsAsync(int classId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommentDto>> GetCommentsAsync(int classId, int userId, string role, CancellationToken cancellationToken = default);
    Task<CommentDto?> AddCommentAsync(int classId, int userId, string role, string content, CancellationToken cancellationToken = default);
    Task<CommentReplyDto?> AddReplyAsync(int commentId, int userId, string content, CancellationToken cancellationToken = default);
    Task<bool> PinCommentAsync(int commentId, CancellationToken cancellationToken = default);
    Task<bool> DeleteCommentAsync(int commentId, int userId, string role, CancellationToken cancellationToken = default);
}

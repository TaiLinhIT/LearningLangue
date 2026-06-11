using LanguageLearning.Application.Features.Classes;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Services;

public sealed class ClassroomService(LanguageLearningDbContext db) : IClassroomService
{
    public async Task<IReadOnlyList<ClassDto>> GetClassesAsync(
        int? userId,
        string? role,
        CancellationToken cancellationToken = default)
    {
        var query = db.Classes.AsNoTracking().AsQueryable();
        if (userId.HasValue && role == Roles.Student)
        {
            query = query.Where(x => x.Students.Any(student => student.StudentId == userId.Value));
        }
        else if (userId.HasValue && role == Roles.Teacher)
        {
            query = query.Where(x => x.TeacherId == userId.Value);
        }

        return await Project(query.OrderByDescending(x => x.IsActive).ThenBy(x => x.Name))
            .ToListAsync(cancellationToken);
    }

    public Task<ClassDto?> GetClassAsync(int id, CancellationToken cancellationToken = default) =>
        Project(db.Classes.AsNoTracking().Where(x => x.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ClassDto> CreateAsync(
        SaveClassCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateDates(command.StartDate, command.EndDate);
        var learningClass = new LearningClass();
        Apply(learningClass, command);
        db.Classes.Add(learningClass);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetClassAsync(learningClass.Id, cancellationToken))!;
    }

    public async Task<ClassDto?> UpdateAsync(
        int id,
        SaveClassCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateDates(command.StartDate, command.EndDate);
        var learningClass = await db.Classes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (learningClass is null)
        {
            return null;
        }

        Apply(learningClass, command);
        await db.SaveChangesAsync(cancellationToken);
        return await GetClassAsync(id, cancellationToken);
    }

    public async Task<bool> AssignStudentAsync(
        int classId,
        AssignStudentCommand command,
        CancellationToken cancellationToken = default)
    {
        var learningClass = await db.Classes.FirstOrDefaultAsync(x => x.Id == classId, cancellationToken);
        var student = await db.Users.FirstOrDefaultAsync(
            x => x.Id == command.StudentId && x.Role == Roles.Student && x.IsActive,
            cancellationToken);
        if (learningClass is null || student is null)
        {
            return false;
        }

        if (!await db.ClassStudents.AnyAsync(
            x => x.ClassId == classId && x.StudentId == command.StudentId,
            cancellationToken))
        {
            db.ClassStudents.Add(new ClassStudent
            {
                ClassId = classId,
                StudentId = command.StudentId
            });
        }

        if (command.CourseId.HasValue && !await db.CourseEnrollments.AnyAsync(
            x => x.CourseId == command.CourseId.Value && x.StudentId == command.StudentId,
            cancellationToken))
        {
            db.CourseEnrollments.Add(new CourseEnrollment
            {
                CourseId = command.CourseId.Value,
                StudentId = command.StudentId,
                ClassId = classId
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ClassStudentDto>> GetStudentsAsync(
        int classId,
        CancellationToken cancellationToken = default) =>
        await db.ClassStudents.AsNoTracking()
            .Where(x => x.ClassId == classId)
            .OrderBy(x => x.Student!.FullName)
            .Select(x => new ClassStudentDto(
                x.StudentId,
                x.Student!.FullName,
                x.Student.Email,
                x.Class!.Level,
                x.JoinedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(
        int classId,
        int userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        await EnsureClassAccessAsync(classId, userId, role, cancellationToken);
        return await db.ClassComments.AsNoTracking()
            .Where(x => x.ClassId == classId && !x.IsDeleted)
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new CommentDto(
                x.Id,
                x.ClassId,
                x.UserId,
                x.User!.FullName,
                x.User.Role,
                x.Content,
                x.IsPinned,
                x.CreatedAt,
                x.Replies
                    .Where(reply => !reply.IsDeleted)
                    .OrderBy(reply => reply.CreatedAt)
                    .Select(reply => new CommentReplyDto(
                        reply.Id,
                        reply.UserId,
                        reply.User!.FullName,
                        reply.Content,
                        reply.CreatedAt))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<CommentDto?> AddCommentAsync(
        int classId,
        int userId,
        string role,
        string content,
        CancellationToken cancellationToken = default)
    {
        await EnsureClassAccessAsync(classId, userId, role, cancellationToken);
        var comment = new ClassComment
        {
            ClassId = classId,
            UserId = userId,
            Content = content.Trim()
        };
        db.ClassComments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetCommentsAsync(classId, userId, role, cancellationToken))
            .First(x => x.Id == comment.Id);
    }

    public async Task<CommentReplyDto?> AddReplyAsync(
        int commentId,
        int userId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var comment = await db.ClassComments.FirstOrDefaultAsync(
            x => x.Id == commentId && !x.IsDeleted,
            cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken);
        if (comment is null || user is null)
        {
            return null;
        }

        await EnsureClassAccessAsync(comment.ClassId, userId, user.Role, cancellationToken);
        var reply = new ClassCommentReply
        {
            CommentId = commentId,
            UserId = userId,
            Content = content.Trim()
        };
        db.ClassCommentReplies.Add(reply);
        await db.SaveChangesAsync(cancellationToken);
        return new CommentReplyDto(reply.Id, userId, user.FullName, reply.Content, reply.CreatedAt);
    }

    public async Task<bool> PinCommentAsync(
        int commentId,
        CancellationToken cancellationToken = default)
    {
        var comment = await db.ClassComments.FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken);
        if (comment is null)
        {
            return false;
        }

        comment.IsPinned = !comment.IsPinned;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteCommentAsync(
        int commentId,
        int userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var comment = await db.ClassComments.FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken);
        if (comment is null
            || (comment.UserId != userId && role is not Roles.Admin and not Roles.Teacher))
        {
            return false;
        }

        comment.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureClassAccessAsync(
        int classId,
        int userId,
        string role,
        CancellationToken cancellationToken)
    {
        var hasAccess = role == Roles.Admin
            || await db.Classes.AnyAsync(
                x => x.Id == classId
                    && (x.TeacherId == userId || x.Students.Any(student => student.StudentId == userId)),
                cancellationToken);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Ban khong thuoc lop hoc nay.");
        }
    }

    private static IQueryable<ClassDto> Project(IQueryable<LearningClass> query) =>
        query.Select(x => new ClassDto(
            x.Id,
            x.Name,
            x.Level,
            x.TeacherId,
            x.Teacher != null ? x.Teacher.FullName : null,
            x.StartDate,
            x.EndDate,
            x.IsActive,
            x.Students.Count));

    private static void Apply(LearningClass learningClass, SaveClassCommand command)
    {
        learningClass.Name = command.Name.Trim();
        learningClass.Level = command.Level.Trim().ToUpperInvariant();
        learningClass.TeacherId = command.TeacherId;
        learningClass.StartDate = command.StartDate;
        learningClass.EndDate = command.EndDate;
        learningClass.IsActive = command.IsActive;
    }

    private static void ValidateDates(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
        {
            throw new ArgumentException("EndDate must be later than StartDate.");
        }
    }
}

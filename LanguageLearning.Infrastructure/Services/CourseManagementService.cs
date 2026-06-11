using LanguageLearning.Application.Features.Courses;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Services;

public sealed class CourseManagementService(LanguageLearningDbContext db) : ICourseManagementService
{
    public async Task<IReadOnlyList<CourseSummaryDto>> GetAsync(
        bool includeDrafts,
        CancellationToken cancellationToken = default)
    {
        var query = db.Courses.AsNoTracking().AsQueryable();
        if (!includeDrafts)
        {
            query = query.Where(x => x.IsPublished);
        }

        return await Project(query.OrderBy(x => x.Title)).ToListAsync(cancellationToken);
    }

    public Task<CourseSummaryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Project(db.Courses.AsNoTracking().Where(x => x.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<CourseSummaryDto> CreateAsync(
        SaveCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var course = new Course
        {
            LanguageId = command.LanguageId,
            Title = command.Title.Trim(),
            Description = command.Description.Trim(),
            Level = command.Level.Trim().ToUpperInvariant(),
            ThumbnailUrl = command.ThumbnailUrl?.Trim(),
            IsPublished = command.IsPublished
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(course.Id, cancellationToken))!;
    }

    public async Task<CourseSummaryDto?> UpdateAsync(
        int id,
        SaveCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (course is null)
        {
            return null;
        }

        course.LanguageId = command.LanguageId;
        course.Title = command.Title.Trim();
        course.Description = command.Description.Trim();
        course.Level = command.Level.Trim().ToUpperInvariant();
        course.ThumbnailUrl = command.ThumbnailUrl?.Trim();
        course.IsPublished = command.IsPublished;
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (course is null)
        {
            return false;
        }

        db.Courses.Remove(course);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (course is null)
        {
            return false;
        }

        course.IsPublished = true;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> CreateUnitAsync(
        SaveUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        var unit = new Unit
        {
            CourseId = command.CourseId,
            Title = command.Title.Trim(),
            SortOrder = command.SortOrder
        };
        db.Units.Add(unit);
        await db.SaveChangesAsync(cancellationToken);
        return unit.Id;
    }

    public async Task<int> CreateLessonAsync(
        SaveLessonCommand command,
        CancellationToken cancellationToken = default)
    {
        var lesson = new Lesson
        {
            UnitId = command.UnitId,
            Title = command.Title.Trim(),
            Description = command.Description.Trim(),
            LessonType = command.LessonType,
            XPReward = command.XPReward,
            SortOrder = command.SortOrder,
            IsLocked = command.IsLocked
        };
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(cancellationToken);
        return lesson.Id;
    }

    private static IQueryable<CourseSummaryDto> Project(IQueryable<Course> query) =>
        query.Select(course => new CourseSummaryDto(
            course.Id,
            course.LanguageId,
            course.Language!.Name,
            course.Title,
            course.Description,
            course.Level,
            course.ThumbnailUrl,
            course.IsPublished,
            course.Units.Count,
            course.Units.SelectMany(x => x.Lessons).Count()));
}

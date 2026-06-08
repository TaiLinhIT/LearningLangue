using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Services;

public class AdminLearningService(LanguageLearningDbContext db) : IAdminLearningService
{
    public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        await db.Users.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Course>> GetCoursesAsync(CancellationToken cancellationToken = default) =>
        await db.Courses.Include(x => x.Language).AsNoTracking().OrderBy(x => x.Title).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Unit>> GetUnitsAsync(CancellationToken cancellationToken = default) =>
        await db.Units.Include(x => x.Course).AsNoTracking().OrderBy(x => x.CourseId).ThenBy(x => x.SortOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetLessonsAsync(CancellationToken cancellationToken = default) =>
        await db.Lessons.Include(x => x.Unit).ThenInclude(x => x!.Course).AsNoTracking().OrderBy(x => x.UnitId).ThenBy(x => x.SortOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Vocabulary>> GetVocabularyAsync(CancellationToken cancellationToken = default) =>
        await db.Vocabulary.Include(x => x.Lesson).AsNoTracking().OrderBy(x => x.Word).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Question>> GetQuestionsAsync(CancellationToken cancellationToken = default) =>
        await db.Questions.Include(x => x.Lesson).Include(x => x.Options).AsNoTracking().OrderBy(x => x.LessonId).ToListAsync(cancellationToken);

    public async Task<Course> SaveCourseAsync(Course course, CancellationToken cancellationToken = default)
    {
        db.Update(course);
        await db.SaveChangesAsync(cancellationToken);
        return course;
    }

    public async Task<Unit> SaveUnitAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        db.Update(unit);
        await db.SaveChangesAsync(cancellationToken);
        return unit;
    }

    public async Task<Lesson> SaveLessonAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        db.Update(lesson);
        await db.SaveChangesAsync(cancellationToken);
        return lesson;
    }

    public async Task<Vocabulary> SaveVocabularyAsync(Vocabulary vocabulary, CancellationToken cancellationToken = default)
    {
        db.Update(vocabulary);
        await db.SaveChangesAsync(cancellationToken);
        return vocabulary;
    }

    public async Task<Question> SaveQuestionAsync(Question question, CancellationToken cancellationToken = default)
    {
        db.Update(question);
        await db.SaveChangesAsync(cancellationToken);
        return question;
    }

    public async Task ToggleCoursePublishedAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.FirstAsync(x => x.Id == courseId, cancellationToken);
        course.IsPublished = !course.IsPublished;
        await db.SaveChangesAsync(cancellationToken);
    }
}

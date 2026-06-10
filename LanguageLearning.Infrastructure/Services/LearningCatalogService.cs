using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Services;

public class LearningCatalogService(LanguageLearningDbContext db) : ILearningCatalogService
{
    public async Task<DashboardSummary> GetDashboardAsync(int userId, CancellationToken cancellationToken = default)
    {
        var progress = await db.UserLessonProgress.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        var streak = await db.Streaks.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        var xp = progress.Sum(x => x.XP);
        var completed = progress.Count(x => x.Status == ProgressStatuses.Completed);
        var average = progress.Count == 0 ? 0 : (int)Math.Round(progress.Average(x => x.Score));
        var rank = Math.Max(1, 50 - (xp / 25));

        return new DashboardSummary(xp, streak?.CurrentStreak ?? 0, completed, average, rank);
    }

    public async Task<IReadOnlyList<Language>> GetLanguagesAsync(CancellationToken cancellationToken = default) =>
        await db.Languages.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Course>> GetCoursesAsync(bool includeDrafts = false, CancellationToken cancellationToken = default)
    {
        var query = db.Courses.Include(x => x.Language).Include(x => x.Units).ThenInclude(x => x.Lessons).AsNoTracking();
        if (!includeDrafts)
        {
            query = query.Where(x => x.IsPublished);
        }

        return await query.OrderBy(x => x.Language!.Name).ThenBy(x => x.Level).ToListAsync(cancellationToken);
    }

    public async Task<Course?> GetCourseAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Courses
            .Include(x => x.Language)
            .Include(x => x.Units.OrderBy(u => u.SortOrder))
            .ThenInclude(x => x.Lessons.OrderBy(l => l.SortOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Lesson?> GetLessonAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Lessons
            .Include(x => x.Unit)
            .ThenInclude(x => x!.Course)
            .Include(x => x.Vocabulary)
            .Include(x => x.Steps.OrderBy(x => x.SortOrder))
            .Include(x => x.Questions)
            .ThenInclude(x => x.Options)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UserLessonProgress>> GetProgressAsync(int userId, CancellationToken cancellationToken = default) =>
        await db.UserLessonProgress
            .Include(x => x.Lesson)
            .ThenInclude(x => x!.Unit)
            .ThenInclude(x => x!.Course)
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CompletedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserAnswer>> GetMistakesAsync(int userId, CancellationToken cancellationToken = default) =>
        await db.UserAnswers
            .Include(x => x.Question)
            .ThenInclude(x => x!.Lesson)
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsCorrect)
            .OrderByDescending(x => x.AnsweredAt)
            .ToListAsync(cancellationToken);

    public async Task<QuizResult> SubmitQuizAsync(int userId, QuizSubmission submission, CancellationToken cancellationToken = default)
    {
        var lesson = await db.Lessons
            .Include(x => x.Unit)
            .Include(x => x.Questions)
            .ThenInclude(x => x.Options)
            .FirstAsync(x => x.Id == submission.LessonId, cancellationToken);

        var total = lesson.Questions.Count;
        var correct = 0;

        foreach (var question in lesson.Questions)
        {
            submission.Answers.TryGetValue(question.Id, out var selected);
            var isCorrect = string.Equals(selected, question.CorrectAnswer, StringComparison.OrdinalIgnoreCase);
            if (isCorrect)
            {
                correct++;
            }

            db.UserAnswers.Add(new UserAnswer
            {
                UserId = userId,
                QuestionId = question.Id,
                SelectedAnswer = selected ?? string.Empty,
                IsCorrect = isCorrect,
                AnsweredAt = DateTime.UtcNow
            });
        }

        var score = total == 0 ? 0 : (int)Math.Round(correct * 100m / total);
        var passed = score >= 70;
        var earnedXp = passed ? lesson.XPReward : Math.Max(5, lesson.XPReward / 3);

        var progress = await db.UserLessonProgress.FirstOrDefaultAsync(
            x => x.UserId == userId && x.LessonId == lesson.Id,
            cancellationToken);

        if (progress is null)
        {
            progress = new UserLessonProgress { UserId = userId, LessonId = lesson.Id };
            db.UserLessonProgress.Add(progress);
        }

        progress.Status = passed ? ProgressStatuses.Completed : ProgressStatuses.InProgress;
        progress.Score = Math.Max(progress.Score, score);
        progress.XP += earnedXp;
        progress.CompletedAt = passed ? DateTime.UtcNow : progress.CompletedAt;

        await UpdateStreakAsync(userId, cancellationToken);

        int? nextLessonId = null;
        if (passed)
        {
            var nextLesson = await db.Lessons
                .Where(x => x.UnitId == lesson.UnitId && x.SortOrder > lesson.SortOrder)
                .OrderBy(x => x.SortOrder)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextLesson is not null)
            {
                nextLesson.IsLocked = false;
                nextLessonId = nextLesson.Id;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new QuizResult(score, earnedXp, passed, correct, total, nextLessonId);
    }

    private async Task UpdateStreakAsync(int userId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var streak = await db.Streaks.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (streak is null)
        {
            db.Streaks.Add(new Streak { UserId = userId, CurrentStreak = 1, LongestStreak = 1, LastStudyDate = today });
            return;
        }

        if (streak.LastStudyDate?.Date == today)
        {
            return;
        }

        streak.CurrentStreak = streak.LastStudyDate?.Date == today.AddDays(-1) ? streak.CurrentStreak + 1 : 1;
        streak.LongestStreak = Math.Max(streak.LongestStreak, streak.CurrentStreak);
        streak.LastStudyDate = today;
    }
}

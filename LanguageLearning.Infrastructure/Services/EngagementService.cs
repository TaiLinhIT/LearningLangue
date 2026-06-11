using LanguageLearning.Application.Features.Engagement;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Services;

public sealed class EngagementService(LanguageLearningDbContext db) : IEngagementService
{
    public async Task<IReadOnlyList<RankingDto>> GetClassRankingAsync(
        int classId,
        CancellationToken cancellationToken = default)
    {
        var studentIds = await db.ClassStudents
            .Where(x => x.ClassId == classId)
            .Select(x => x.StudentId)
            .ToListAsync(cancellationToken);
        return await BuildRankingAsync(studentIds, cancellationToken);
    }

    public async Task<IReadOnlyList<RankingDto>> GetCenterRankingAsync(
        CancellationToken cancellationToken = default)
    {
        var studentIds = await db.Users
            .Where(x => x.Role == Roles.Student && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        return await BuildRankingAsync(studentIds, cancellationToken);
    }

    public async Task<RankingDto?> GetStudentRankingAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        (await GetCenterRankingAsync(cancellationToken)).FirstOrDefault(x => x.StudentId == userId);

    public async Task<IReadOnlyList<RewardDto>> GetRewardsAsync(
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var rewards = await db.Rewards.AsNoTracking().OrderBy(x => x.Title).ToListAsync(cancellationToken);
        var statuses = userId.HasValue
            ? await db.StudentRewards.AsNoTracking()
                .Where(x => x.StudentId == userId.Value)
                .ToDictionaryAsync(x => x.RewardId, x => x.Status, cancellationToken)
            : [];
        return rewards.Select(x => new RewardDto(
            x.Id,
            x.Title,
            x.Description,
            x.RewardType,
            x.RequiredRank,
            x.RequiredPoint,
            x.CourseId,
            x.ClassId,
            x.IsActive,
            statuses.GetValueOrDefault(x.Id))).ToList();
    }

    public async Task<RewardDto> CreateRewardAsync(
        SaveRewardCommand command,
        CancellationToken cancellationToken = default)
    {
        var reward = new Reward
        {
            Title = command.Title.Trim(),
            Description = command.Description.Trim(),
            RewardType = command.RewardType.Trim(),
            RequiredRank = command.RequiredRank,
            RequiredPoint = command.RequiredPoint,
            CourseId = command.CourseId,
            ClassId = command.ClassId,
            IsActive = command.IsActive
        };
        db.Rewards.Add(reward);
        await db.SaveChangesAsync(cancellationToken);
        return new RewardDto(
            reward.Id,
            reward.Title,
            reward.Description,
            reward.RewardType,
            reward.RequiredRank,
            reward.RequiredPoint,
            reward.CourseId,
            reward.ClassId,
            reward.IsActive,
            null);
    }

    public async Task<int> CalculateRewardsAsync(CancellationToken cancellationToken = default)
    {
        var rankings = await GetCenterRankingAsync(cancellationToken);
        var rewards = await db.Rewards.Where(x => x.IsActive).ToListAsync(cancellationToken);
        var awarded = 0;
        foreach (var ranking in rankings)
        {
            foreach (var reward in rewards)
            {
                var eligible = (!reward.RequiredRank.HasValue || ranking.Rank <= reward.RequiredRank.Value)
                    && (!reward.RequiredPoint.HasValue || ranking.TotalPoint >= reward.RequiredPoint.Value);
                if (!eligible || await db.StudentRewards.AnyAsync(
                    x => x.StudentId == ranking.StudentId && x.RewardId == reward.Id,
                    cancellationToken))
                {
                    continue;
                }

                db.StudentRewards.Add(new StudentReward
                {
                    StudentId = ranking.StudentId,
                    RewardId = reward.Id
                });
                db.Notifications.Add(new Notification
                {
                    UserId = ranking.StudentId,
                    Title = "Ban da nhan thuong",
                    Message = $"Chuc mung! Ban da dat phan thuong {reward.Title}.",
                    Type = "Reward"
                });
                awarded++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return awarded;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await db.Notifications.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationDto(
                x.Id,
                x.Title,
                x.Message,
                x.Type,
                x.IsRead,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<bool> MarkNotificationReadAsync(
        int userId,
        int notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(
            x => x.Id == notificationId && x.UserId == userId,
            cancellationToken);
        if (notification is null)
        {
            return false;
        }

        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<IpaSoundDto>> GetIpaSoundsAsync(
        CancellationToken cancellationToken = default) =>
        await db.IPASounds.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Select(x => new IpaSoundDto(
                x.Id,
                x.Symbol,
                x.SoundType,
                x.ExampleWord,
                x.AudioUrl,
                x.VideoUrl,
                x.VietnameseGuide,
                x.ComparisonNote,
                x.SortOrder))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<IpaLessonDto>> GetIpaLessonsAsync(
        CancellationToken cancellationToken = default) =>
        await db.IPALessons.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Select(x => new IpaLessonDto(
                x.Id,
                x.Title,
                x.Description,
                x.SoundType,
                x.SortOrder,
                x.Exercises.Count))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<IpaExerciseDto>> GetIpaExercisesAsync(
        CancellationToken cancellationToken = default) =>
        await db.IPAExercises.AsNoTracking()
            .OrderBy(x => x.IPALesson!.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => new IpaExerciseDto(
                x.Id,
                x.IPALessonId,
                x.QuestionText,
                x.CorrectAnswer,
                x.AudioUrl,
                x.Explanation))
            .ToListAsync(cancellationToken);

    public async Task<IpaSubmissionResult?> SubmitIpaExerciseAsync(
        int exerciseId,
        string answer,
        CancellationToken cancellationToken = default)
    {
        var exercise = await db.IPAExercises.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == exerciseId, cancellationToken);
        return exercise is null
            ? null
            : new IpaSubmissionResult(
                string.Equals(exercise.CorrectAnswer.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase),
                exercise.Explanation);
    }

    public async Task<ReportDto> CreateReportAsync(
        int userId,
        string targetType,
        int targetId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var report = new Report
        {
            ReporterId = userId,
            TargetType = targetType.Trim(),
            TargetId = targetId,
            Reason = reason.Trim()
        };
        db.Reports.Add(report);
        await db.SaveChangesAsync(cancellationToken);
        var reporterName = await db.Users.Where(x => x.Id == userId)
            .Select(x => x.FullName)
            .FirstAsync(cancellationToken);
        return new ReportDto(
            report.Id,
            userId,
            reporterName,
            report.TargetType,
            report.TargetId,
            report.Reason,
            report.Status,
            report.CreatedAt);
    }

    public async Task<IReadOnlyList<ReportDto>> GetReportsAsync(
        CancellationToken cancellationToken = default) =>
        await db.Reports.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ReportDto(
                x.Id,
                x.ReporterId,
                x.Reporter!.FullName,
                x.TargetType,
                x.TargetId,
                x.Reason,
                x.Status,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<RankingDto>> BuildRankingAsync(
        IReadOnlyCollection<int> studentIds,
        CancellationToken cancellationToken)
    {
        var students = await db.Users.AsNoTracking()
            .Where(x => studentIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var progress = await db.UserLessonProgress.AsNoTracking()
            .Where(x => studentIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);
        var aiScores = await db.SentencePractices.AsNoTracking()
            .Where(x => studentIds.Contains(x.UserId) && x.ScoringResult != null)
            .Select(x => new { x.UserId, x.ScoringResult!.OverallScore })
            .ToListAsync(cancellationToken);
        var streaks = await db.Streaks.AsNoTracking()
            .Where(x => studentIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, x => x.CurrentStreak, cancellationToken);

        var rows = studentIds.Select(id =>
        {
            var userProgress = progress.Where(x => x.UserId == id).ToList();
            var xp = userProgress.Sum(x => x.XP);
            var quiz = userProgress.Count == 0 ? 0 : (int)Math.Round(userProgress.Average(x => x.Score));
            var ai = aiScores.Where(x => x.UserId == id).Select(x => x.OverallScore).DefaultIfEmpty().Max();
            var completion = userProgress.Count(x => x.Status == ProgressStatuses.Completed) * 20;
            return new
            {
                StudentId = id,
                StudentName = students.GetValueOrDefault(id)?.FullName ?? "Student",
                XP = xp,
                Quiz = quiz,
                AI = ai,
                Completion = completion,
                Total = xp + quiz + ai + completion,
                Streak = streaks.GetValueOrDefault(id)
            };
        }).OrderByDescending(x => x.Total).ThenByDescending(x => x.Streak).ToList();

        return rows.Select((x, index) => new RankingDto(
            index + 1,
            x.StudentId,
            x.StudentName,
            x.XP,
            x.Quiz,
            x.AI,
            x.Completion,
            x.Total,
            x.Streak)).ToList();
    }
}

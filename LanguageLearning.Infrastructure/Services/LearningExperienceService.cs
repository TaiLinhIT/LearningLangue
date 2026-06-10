using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Services;

public class LearningExperienceService(LanguageLearningDbContext db) : ILearningExperienceService
{
    private const string DefaultClassName = "A1 Morning";

    public async Task<IReadOnlyList<VocabularyLibraryItem>> GetVocabularyLibraryAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var progress = await db.UserLessonProgress
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.LessonId, cancellationToken);

        var words = await db.Vocabulary
            .Include(x => x.Lesson)
            .ThenInclude(x => x!.Unit)
            .ThenInclude(x => x!.Course)
            .AsNoTracking()
            .OrderBy(x => x.Lesson!.Unit!.Course!.Level)
            .ThenBy(x => x.Lesson!.Title)
            .ThenBy(x => x.Word)
            .ToListAsync(cancellationToken);

        return words
            .Select(word =>
            {
                progress.TryGetValue(word.LessonId, out var lessonProgress);
                var status = lessonProgress?.Status switch
                {
                    ProgressStatuses.Completed => "Mastered",
                    ProgressStatuses.InProgress => "Learning",
                    _ => "New"
                };

                return new VocabularyLibraryItem(
                    word.Id,
                    word.Word,
                    word.Meaning,
                    word.Pronunciation,
                    word.ExampleSentence,
                    word.AudioUrl,
                    word.ImageUrl,
                    word.Lesson?.Unit?.Course?.Title ?? "Course",
                    word.Lesson?.Title ?? "Lesson",
                    word.Lesson?.Unit?.Course?.Level ?? "A1",
                    status);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<RankingEntry>> GetClassRankingAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await GetCenterRankingAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RankingEntry>> GetCenterRankingAsync(
        CancellationToken cancellationToken = default)
    {
        var students = await db.Users
            .AsNoTracking()
            .Where(x => x.Role == Roles.Student && x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);

        var progress = await db.UserLessonProgress
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var progressByUser = progress
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var streaks = await db.Streaks
            .AsNoTracking()
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

        var sentenceScores = await db.SentencePractices
            .Include(x => x.ScoringResult)
            .AsNoTracking()
            .Where(x => x.ScoringResult != null)
            .ToListAsync(cancellationToken);
        var aiPracticeScoreByUser = sentenceScores
            .GroupBy(x => x.UserId)
            .ToDictionary(
                x => x.Key,
                x => x.Max(score => score.ScoringResult!.OverallScore));

        var ranked = students
            .Select(student =>
            {
                progressByUser.TryGetValue(student.Id, out var userProgress);
                userProgress ??= [];
                var completed = userProgress.Count(x => x.Status == ProgressStatuses.Completed);
                var xp = userProgress.Sum(x => x.XP);
                var quizScore = userProgress.Count == 0
                    ? 0
                    : (int)Math.Round(userProgress.Average(x => x.Score));
                var aiPracticeScore = aiPracticeScoreByUser.GetValueOrDefault(student.Id);
                var completionPoint = completed * 20;
                var totalPoint = xp + quizScore + aiPracticeScore + completionPoint;
                var streak = streaks.TryGetValue(student.Id, out var streakRow)
                    ? streakRow.CurrentStreak
                    : 0;

                return new
                {
                    Student = student,
                    XP = xp,
                    QuizScore = quizScore,
                    AIPracticeScore = aiPracticeScore,
                    CompletionPoint = completionPoint,
                    TotalPoint = totalPoint,
                    Streak = streak,
                    Completed = completed
                };
            })
            .OrderByDescending(x => x.TotalPoint)
            .ThenByDescending(x => x.Streak)
            .ThenBy(x => x.Student.FullName)
            .ToList();

        return ranked
            .Select((row, index) => new RankingEntry(
                index + 1,
                row.Student.Id,
                row.Student.FullName,
                DefaultClassName,
                row.XP,
                row.QuizScore,
                row.AIPracticeScore,
                row.CompletionPoint,
                row.TotalPoint,
                row.Streak))
            .ToList();
    }

    public async Task<IReadOnlyList<RewardStatusItem>> GetRewardsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var ranking = await GetCenterRankingAsync(cancellationToken);
        var me = ranking.FirstOrDefault(x => x.StudentId == userId);
        var progress = await db.UserLessonProgress
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        var completedLessons = progress.Count(x => x.Status == ProgressStatuses.Completed);
        var totalScore = progress.Sum(x => x.Score);

        return
        [
            new RewardStatusItem(
                1,
                "Top A1 Morning",
                "Danh cho hoc vien nam trong top 3 cua lop.",
                "Rank <= 3",
                me is not null && me.Rank <= 3 ? "Eligible" : "Not eligible"),
            new RewardStatusItem(
                2,
                "Course Finisher",
                "Hoan thanh it nhat 6 bai hoc trong khoa A1.",
                "Completed lessons >= 6",
                completedLessons >= 6 ? "Eligible" : "Not eligible"),
            new RewardStatusItem(
                3,
                "Score Builder",
                "Tong diem quiz tich luy dat muc yeu cau.",
                "Total score >= 500",
                totalScore >= 500 ? "Eligible" : "Not eligible")
        ];
    }

    public Task<IReadOnlyList<IpaSoundItem>> GetIpaSoundsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IpaSoundItem> sounds =
        [
            new(1, "/i:/", "Vowel", "see", null, null, "Keo dai am i, mieng hoi cuoi, luoi nang cao.", "De nham voi /i/ ngan trong sit."),
            new(2, "/i/", "Vowel", "sit", null, null, "Am i ngan, mieng tha long hon /i:/.", "So sanh see va sit."),
            new(3, "/ae/", "Vowel", "cat", null, null, "Mo mieng rong, am nam giua a va e.", "Khac voi /e/ trong bed."),
            new(4, "/u:/", "Vowel", "food", null, null, "Tron moi va keo dai am u.", "De nham voi /u/ ngan trong good."),
            new(5, "/th/", "Consonant", "think", null, null, "Dat dau luoi giua hai ham rang va day hoi nhe.", "Khac voi /t/ trong tin."),
            new(6, "/dh/", "Consonant", "this", null, null, "Giong /th/ nhung co rung day thanh.", "Khac voi /d/ trong day."),
            new(7, "/sh/", "Consonant", "ship", null, null, "Day hoi qua khe hep, moi hoi tron.", "So sanh ship va sip."),
            new(8, "/ch/", "Consonant", "chair", null, null, "Bat dau nhu /t/ va ket thuc bang /sh/.", "So sanh chair va share.")
        ];

        return Task.FromResult(sounds);
    }

    public async Task<ClassOverview> GetClassOverviewAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var ranking = await GetCenterRankingAsync(cancellationToken);
        var students = ranking
            .Select(x => new ClassmateItem(
                x.StudentId,
                x.StudentName,
                x.XP,
                x.CompletionPoint / 20))
            .ToList();

        var teacherName = await db.Users
            .AsNoTracking()
            .Where(x => x.Role == Roles.Teacher && x.IsActive)
            .OrderBy(x => x.FullName)
            .Select(x => x.FullName)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Teacher Demo";

        var comments = new List<ClassCommentItem>
        {
            new(
                1,
                teacherName,
                "Neu chua phan biet duoc see va sit, hay nghe lai cap am /i:/ va /i/ trong phan IPA.",
                true,
                DateTime.UtcNow.AddHours(-6),
                [
                    new ClassCommentReplyItem(
                        1,
                        "Linh Nguyen",
                        "Em da nghe lai va thay /i:/ can keo dai hon.",
                        DateTime.UtcNow.AddHours(-5))
                ]),
            new(
                2,
                "Linh Nguyen",
                "Cau 'My name Linh' thieu dong tu to be dung khong thay?",
                false,
                DateTime.UtcNow.AddHours(-2),
                [
                    new ClassCommentReplyItem(
                        2,
                        teacherName,
                        "Dung roi. Cau tu nhien hon la 'My name is Linh.'",
                        DateTime.UtcNow.AddHours(-1))
                ])
        };

        return new ClassOverview(
            DefaultClassName,
            "A1",
            teacherName,
            DateTime.UtcNow.Date.AddDays(-14),
            DateTime.UtcNow.Date.AddMonths(2),
            students,
            comments);
    }
}

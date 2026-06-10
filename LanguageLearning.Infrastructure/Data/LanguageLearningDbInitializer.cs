using System.Data;
using LanguageLearning.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.Infrastructure.Data;

public static class LanguageLearningDbInitializer
{
    private const string InitialMigrationId = "20260608061602_InitialCreate";
    private const string AddUserLearningGoalMigrationId = "20260609000000_AddUserLearningGoal";
    private const string InitialMigrationProductVersion = "8.0.22";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LanguageLearningDbContext>();
        await EnsureDatabaseReadyAsync(db);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var hasher = new PasswordHasher<User>();

        await EnsureUserAsync(db, hasher, "Admin", "admin@linguaflow.local", Roles.Admin, "https://i.pravatar.cc/120?img=12", "Quan tri he thong", "Admin@123");
        var learner = await EnsureUserAsync(db, hasher, "Linh Nguyen", "learner@linguaflow.local", Roles.Learner, "https://i.pravatar.cc/120?img=32", "Giao tiep hang ngay", "Learner@123");
        var student2 = await EnsureUserAsync(db, hasher, "Minh Tran", "minh@linguaflow.local", Roles.Student, null, "Giao tiep hang ngay", "Student@123");
        var student3 = await EnsureUserAsync(db, hasher, "Hana Le", "hana@linguaflow.local", Roles.Student, null, "Thi chung chi", "Student@123");
        var student4 = await EnsureUserAsync(db, hasher, "An Pham", "an@linguaflow.local", Roles.Student, null, "Cong viec", "Student@123");
        var student5 = await EnsureUserAsync(db, hasher, "Bao Hoang", "bao@linguaflow.local", Roles.Student, null, "Du hoc", "Student@123");
        await EnsureUserAsync(db, hasher, "Teacher Demo", "teacher@linguaflow.local", Roles.Teacher, null, "Giang day khoa hoc", "Teacher@123");
        await EnsureUserAsync(db, hasher, "Receptionist Demo", "reception@linguaflow.local", Roles.Receptionist, null, "Cham soc hoc vien", "Reception@123");

        await EnsureStudentSeedDataAsync(db, [learner.Id, student2.Id, student3.Id, student4.Id, student5.Id]);
        await transaction.CommitAsync();
    }

    private static async Task<User> EnsureUserAsync(
        LanguageLearningDbContext db,
        PasswordHasher<User> hasher,
        string fullName,
        string email,
        string role,
        string? avatarUrl,
        string learningGoal,
        string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingUser = await db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        if (existingUser is not null)
        {
            var changed = false;
            if (string.IsNullOrWhiteSpace(existingUser.LearningGoal))
            {
                existingUser.LearningGoal = learningGoal;
                changed = true;
            }

            if (!existingUser.IsActive)
            {
                existingUser.IsActive = true;
                changed = true;
            }

            if (changed)
            {
                existingUser.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            return existingUser;
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            FullName = fullName,
            Email = normalizedEmail,
            Role = role,
            AvatarUrl = avatarUrl,
            LearningGoal = learningGoal,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.PasswordHash = hasher.HashPassword(user, password);

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync();
            return user;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            db.Entry(user).State = EntityState.Detached;
            return await db.Users.FirstAsync(x => x.Email == normalizedEmail);
        }
    }

    private static async Task EnsureStudentSeedDataAsync(LanguageLearningDbContext db, IReadOnlyList<int> studentIds)
    {
        var lessonIds = await db.Lessons
            .Join(
                db.Units,
                lesson => lesson.UnitId,
                unit => unit.Id,
                (lesson, unit) => new { Lesson = lesson, Unit = unit })
            .Where(x => x.Unit.CourseId == 1)
            .OrderBy(x => x.Unit.SortOrder)
            .ThenBy(x => x.Lesson.SortOrder)
            .Select(x => x.Lesson.Id)
            .Take(3)
            .ToListAsync();

        if (lessonIds.Count == 0)
        {
            lessonIds = await db.Lessons
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(3)
                .ToListAsync();
        }

        var firstLessonId = lessonIds.FirstOrDefault();
        List<LessonStepSeed> firstLessonStepSeed = firstLessonId == 0
            ? []
            : await db.LessonSteps
                .Where(x => x.LessonId == firstLessonId)
                .OrderBy(x => x.SortOrder)
                .Select(x => new LessonStepSeed(x.Id, x.SortOrder, x.MinScoreToPass))
                .ToListAsync();

        var sentencePracticeStepId = await db.LessonSteps
            .Where(x => x.StepType == LessonStepTypes.SentencePractice)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        for (var index = 0; index < studentIds.Count; index++)
        {
            var studentId = studentIds[index];
            var course = await db.UserCourses.FirstOrDefaultAsync(x => x.UserId == studentId && x.CourseId == 1);
            if (course is null)
            {
                db.UserCourses.Add(new UserCourse
                {
                    UserId = studentId,
                    CourseId = 1,
                    CurrentLessonId = lessonIds.FirstOrDefault(),
                    StartedAt = DateTime.UtcNow.AddDays(-14 + index)
                });
            }
            else if (course.CurrentLessonId is null or 0)
            {
                course.CurrentLessonId = lessonIds.FirstOrDefault();
            }

            var targetStreak = Math.Max(1, 6 - index);
            var streak = await db.Streaks.FirstOrDefaultAsync(x => x.UserId == studentId);
            if (streak is null)
            {
                db.Streaks.Add(new Streak
                {
                    UserId = studentId,
                    CurrentStreak = targetStreak,
                    LongestStreak = targetStreak + 2,
                    LastStudyDate = DateTime.UtcNow.Date
                });
            }
            else
            {
                streak.CurrentStreak = Math.Max(streak.CurrentStreak, targetStreak);
                streak.LongestStreak = Math.Max(streak.LongestStreak, targetStreak + 2);
                streak.LastStudyDate ??= DateTime.UtcNow.Date;
            }

            var completedCount = Math.Max(0, 3 - index);
            for (var lessonIndex = 0; lessonIndex < lessonIds.Count; lessonIndex++)
            {
                var lessonId = lessonIds[lessonIndex];
                var score = Math.Clamp(96 - (index * 7) - (lessonIndex * 4), 60, 100);
                var completed = lessonIndex < completedCount;
                await EnsureLessonProgressAsync(db, studentId, lessonId, score, completed, lessonIndex, index);
            }

            foreach (var step in firstLessonStepSeed)
            {
                await EnsureStepProgressAsync(db, studentId, step, completedCount > 0, index);
            }

            if (sentencePracticeStepId > 0)
            {
                await EnsureSentencePracticeSeedAsync(db, studentId, sentencePracticeStepId, index);
            }
        }

        var primaryStudentId = studentIds[0];
        if (!await db.Subscriptions.AnyAsync(x => x.UserId == primaryStudentId && x.PlanName == "Plus"))
        {
            db.Subscriptions.Add(new Subscription { UserId = primaryStudentId, PlanName = "Plus", Status = "Active", StartedAt = DateTime.UtcNow.AddDays(-14), ExpiredAt = DateTime.UtcNow.AddMonths(1) });
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureLessonProgressAsync(
        LanguageLearningDbContext db,
        int studentId,
        int lessonId,
        int score,
        bool completed,
        int lessonIndex,
        int studentIndex)
    {
        var status = completed ? ProgressStatuses.Completed : ProgressStatuses.InProgress;
        var targetXp = completed ? 20 + (lessonIndex * 5) : 5;
        DateTime? completedAt = completed
            ? DateTime.UtcNow.AddDays(-lessonIndex - studentIndex)
            : null;
        var progress = await db.UserLessonProgress.FirstOrDefaultAsync(
            x => x.UserId == studentId && x.LessonId == lessonId);

        if (progress is null)
        {
            db.UserLessonProgress.Add(new UserLessonProgress
            {
                UserId = studentId,
                LessonId = lessonId,
                Status = status,
                Score = score,
                XP = targetXp,
                CompletedAt = completedAt
            });
            return;
        }

        progress.Score = Math.Max(progress.Score, score);
        progress.XP = Math.Max(progress.XP, targetXp);

        if (completed)
        {
            progress.Status = ProgressStatuses.Completed;
            progress.CompletedAt ??= completedAt;
        }
        else if (progress.Status == ProgressStatuses.NotStarted)
        {
            progress.Status = ProgressStatuses.InProgress;
        }
    }

    private static async Task EnsureStepProgressAsync(
        LanguageLearningDbContext db,
        int studentId,
        LessonStepSeed step,
        bool completed,
        int studentIndex)
    {
        var targetScore = step.MinScoreToPass == 0
            ? 100
            : Math.Clamp(94 - (studentIndex * 6) - step.SortOrder, step.MinScoreToPass, 100);
        var progressPercent = completed ? 100 : Math.Min(80, step.SortOrder * 12);
        var progress = await db.StudentStepProgress.FirstOrDefaultAsync(
            x => x.UserId == studentId && x.LessonStepId == step.Id);

        if (progress is null)
        {
            db.StudentStepProgress.Add(new StudentStepProgress
            {
                UserId = studentId,
                LessonStepId = step.Id,
                Status = completed ? ProgressStatuses.Completed : ProgressStatuses.InProgress,
                Score = targetScore,
                ProgressPercent = progressPercent,
                StartedAt = DateTime.UtcNow.AddDays(-10 + studentIndex),
                CompletedAt = completed ? DateTime.UtcNow.AddDays(-8 + studentIndex) : null
            });
            return;
        }

        progress.Score = Math.Max(progress.Score, targetScore);
        progress.ProgressPercent = Math.Max(progress.ProgressPercent, progressPercent);
        progress.StartedAt ??= DateTime.UtcNow.AddDays(-10 + studentIndex);
        if (completed)
        {
            progress.Status = ProgressStatuses.Completed;
            progress.CompletedAt ??= DateTime.UtcNow.AddDays(-8 + studentIndex);
        }
        else if (progress.Status == ProgressStatuses.NotStarted)
        {
            progress.Status = ProgressStatuses.InProgress;
        }
    }

    private static async Task EnsureSentencePracticeSeedAsync(
        LanguageLearningDbContext db,
        int studentId,
        int lessonStepId,
        int studentIndex)
    {
        var overall = Math.Clamp(92 - (studentIndex * 5), 68, 100);
        var sentence = studentIndex switch
        {
            0 => "Hello, my name is Linh.",
            1 => "My name is Minh and I am from Vietnam.",
            2 => "Hello teacher, my name is Hana.",
            3 => "My name is An. Nice to meet you.",
            _ => "Hello, my name is Bao."
        };

        var practice = await db.SentencePractices
            .Include(x => x.ScoringResult)
            .Where(x => x.UserId == studentId && x.LessonStepId == lessonStepId)
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync();

        if (practice is null)
        {
            db.SentencePractices.Add(new SentencePractice
            {
                UserId = studentId,
                LessonStepId = lessonStepId,
                GrammarStructure = "My name is + name",
                StudentSentence = sentence,
                SubmittedAt = DateTime.UtcNow.AddDays(-studentIndex),
                ScoringResult = CreateSeedScoringResult(overall)
            });
            return;
        }

        if (practice.ScoringResult is null)
        {
            var scoringResult = CreateSeedScoringResult(overall);
            scoringResult.SentencePracticeId = practice.Id;
            db.AIScoringResults.Add(scoringResult);
            return;
        }

        practice.ScoringResult.GrammarScore = Math.Max(practice.ScoringResult.GrammarScore, overall - 2);
        practice.ScoringResult.VocabularyScore = Math.Max(practice.ScoringResult.VocabularyScore, overall - 1);
        practice.ScoringResult.NaturalnessScore = Math.Max(practice.ScoringResult.NaturalnessScore, overall);
        practice.ScoringResult.OverallScore = Math.Max(practice.ScoringResult.OverallScore, overall);
    }

    private static AIScoringResult CreateSeedScoringResult(int overall) =>
        new()
        {
            GrammarScore = Math.Max(0, overall - 2),
            VocabularyScore = Math.Max(0, overall - 1),
            NaturalnessScore = overall,
            OverallScore = overall,
            Feedback = "Cau dung y, can chu y phat am va do tu nhien khi noi.",
            SuggestedSentence = "Hello, my name is Linh. Nice to meet you.",
            CreatedAt = DateTime.UtcNow
        };

    private sealed record LessonStepSeed(int Id, int SortOrder, int MinScoreToPass);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }

    private static bool IsLearningGoalColumnAlreadyExists(SqlException exception)
    {
        return exception.Number == 2705
            && exception.Message.Contains("LearningGoal", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task EnsureDatabaseReadyAsync(LanguageLearningDbContext db)
    {
        if (await db.Database.CanConnectAsync() && await WasCreatedWithoutMigrationsAsync(db))
        {
            await MarkMigrationAppliedAsync(db, InitialMigrationId);
        }

        try
        {
            await db.Database.MigrateAsync();
        }
        catch (SqlException ex) when (IsLearningGoalColumnAlreadyExists(ex))
        {
            await MarkMigrationAppliedAsync(db, AddUserLearningGoalMigrationId);
        }

        await EnsureUserLearningGoalColumnAsync(db);
        await MarkMigrationAppliedAsync(db, AddUserLearningGoalMigrationId);
    }

    private static Task EnsureUserLearningGoalColumnAsync(LanguageLearningDbContext db)
    {
        return db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH(N'dbo.Users', N'LearningGoal') IS NULL
            BEGIN
                ALTER TABLE [Users]
                ADD [LearningGoal] nvarchar(180) NOT NULL
                    CONSTRAINT [DF_Users_LearningGoal] DEFAULT N'Giao tiep hang ngay';
            END;
            """);
    }

    private static async Task<bool> WasCreatedWithoutMigrationsAsync(LanguageLearningDbContext db)
    {
        var hasLegacySchema = await db.Database.SqlQueryRaw<int>(
            """
            SELECT CAST(CASE
                WHEN OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
                    AND OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
                    AND OBJECT_ID(N'dbo.Languages', N'U') IS NOT NULL
                    AND OBJECT_ID(N'dbo.Courses', N'U') IS NOT NULL
                    AND OBJECT_ID(N'dbo.Lessons', N'U') IS NOT NULL
                THEN 1
                ELSE 0
            END AS int) AS [Value]
            """).SingleAsync();

        return hasLegacySchema == 1;
    }

    private static Task MarkMigrationAppliedAsync(LanguageLearningDbContext db, string migrationId)
    {
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
            BEGIN
                CREATE TABLE [__EFMigrationsHistory] (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
            END;

            IF NOT EXISTS (
                SELECT 1
                FROM [__EFMigrationsHistory]
                WHERE [MigrationId] = {migrationId}
            )
            BEGIN
                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                VALUES ({migrationId}, {InitialMigrationProductVersion});
            END;
            """);
    }
}

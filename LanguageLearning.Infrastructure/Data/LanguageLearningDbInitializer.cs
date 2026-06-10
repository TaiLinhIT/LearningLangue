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
    private const string AdminEmail = "admin@linguaflow.local";
    private const string LearnerEmail = "learner@linguaflow.local";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LanguageLearningDbContext>();
        await EnsureDatabaseReadyAsync(db);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var hasher = new PasswordHasher<User>();
        await EnsureUserAsync(db, hasher, "Admin", AdminEmail, Roles.Admin, "https://i.pravatar.cc/120?img=12", "Quan tri he thong", "Admin@123");
        var learner = await EnsureUserAsync(db, hasher, "Linh Nguyen", LearnerEmail, Roles.Learner, "https://i.pravatar.cc/120?img=32", "Giao tiep hang ngay", "Learner@123");

        await EnsureLearnerSeedDataAsync(db, learner.Id);
        await transaction.CommitAsync();
    }

    private static async Task<User> EnsureUserAsync(
        LanguageLearningDbContext db,
        PasswordHasher<User> hasher,
        string fullName,
        string email,
        string role,
        string avatarUrl,
        string learningGoal,
        string password)
    {
        var existingUser = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (existingUser is not null)
        {
            if (string.IsNullOrWhiteSpace(existingUser.LearningGoal))
            {
                existingUser.LearningGoal = learningGoal;
                await db.SaveChangesAsync();
            }

            return existingUser;
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            Role = role,
            AvatarUrl = avatarUrl,
            LearningGoal = learningGoal,
            CreatedAt = DateTime.UtcNow
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
            return await db.Users.FirstAsync(x => x.Email == email);
        }
    }

    private static async Task EnsureLearnerSeedDataAsync(LanguageLearningDbContext db, int learnerId)
    {
        if (!await db.UserCourses.AnyAsync(x => x.UserId == learnerId && x.CourseId == 1))
        {
            db.UserCourses.Add(new UserCourse { UserId = learnerId, CourseId = 1, CurrentLessonId = 1, StartedAt = DateTime.UtcNow });
        }

        if (!await db.Streaks.AnyAsync(x => x.UserId == learnerId))
        {
            db.Streaks.Add(new Streak { UserId = learnerId, CurrentStreak = 5, LongestStreak = 9, LastStudyDate = DateTime.UtcNow.Date });
        }

        if (!await db.Subscriptions.AnyAsync(x => x.UserId == learnerId && x.PlanName == "Plus"))
        {
            db.Subscriptions.Add(new Subscription { UserId = learnerId, PlanName = "Plus", Status = "Active", StartedAt = DateTime.UtcNow.AddDays(-14), ExpiredAt = DateTime.UtcNow.AddMonths(1) });
        }

        await db.SaveChangesAsync();
    }

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
            await MarkInitialMigrationAppliedAsync(db);
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

    private static Task MarkInitialMigrationAppliedAsync(LanguageLearningDbContext db)
    {
        return MarkMigrationAppliedAsync(db, InitialMigrationId);
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

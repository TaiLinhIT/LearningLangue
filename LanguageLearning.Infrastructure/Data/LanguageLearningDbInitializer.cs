using LanguageLearning.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.Infrastructure.Data;

public static class LanguageLearningDbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LanguageLearningDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (await db.Users.AnyAsync())
        {
            return;
        }

        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            FullName = "Admin",
            Email = "admin@linguaflow.local",
            Role = Roles.Admin,
            AvatarUrl = "https://i.pravatar.cc/120?img=12",
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");

        var learner = new User
        {
            FullName = "Linh Nguyen",
            Email = "learner@linguaflow.local",
            Role = Roles.Learner,
            AvatarUrl = "https://i.pravatar.cc/120?img=32",
            CreatedAt = DateTime.UtcNow
        };
        learner.PasswordHash = hasher.HashPassword(learner, "Learner@123");

        db.Users.AddRange(admin, learner);
        await db.SaveChangesAsync();

        db.UserCourses.Add(new UserCourse { UserId = learner.Id, CourseId = 1, CurrentLessonId = 1, StartedAt = DateTime.UtcNow });
        db.Streaks.Add(new Streak { UserId = learner.Id, CurrentStreak = 5, LongestStreak = 9, LastStudyDate = DateTime.UtcNow.Date });
        db.Subscriptions.Add(new Subscription { UserId = learner.Id, PlanName = "Plus", Status = "Active", StartedAt = DateTime.UtcNow.AddDays(-14), ExpiredAt = DateTime.UtcNow.AddMonths(1) });
        await db.SaveChangesAsync();
    }
}

using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Services;

public class AuthService(LanguageLearningDbContext db) : IAuthService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<User?> ValidateUserAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    public async Task<User> RegisterAsync(
        string fullName,
        string email,
        string password,
        string learningGoal,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (await db.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Email nay da duoc dang ky.");
        }

        var user = new User
        {
            FullName = fullName,
            Email = normalizedEmail,
            Role = Roles.Learner,
            LearningGoal = NormalizeLearningGoal(learningGoal),
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return user;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("Email nay da duoc dang ky.", ex);
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string NormalizeLearningGoal(string learningGoal) =>
        string.IsNullOrWhiteSpace(learningGoal) ? "Giao tiep hang ngay" : learningGoal.Trim();

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}

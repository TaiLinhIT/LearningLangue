using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Services;

public class AuthService(LanguageLearningDbContext db) : IAuthService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<User?> ValidateUserAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task<User> RegisterAsync(string fullName, string email, string password, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new User
        {
            FullName = fullName,
            Email = email.Trim().ToLowerInvariant(),
            Role = Roles.Learner,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }
}

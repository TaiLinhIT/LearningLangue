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
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task<User> RegisterAsync(string fullName, string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new User
        {
            FullName = fullName,
            Email = normalizedEmail,
            Role = Roles.Learner,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<AuthSession?> LoginAsync(
        string email,
        string password,
        DeviceInfo device,
        CancellationToken cancellationToken = default)
    {
        var user = await ValidateUserAsync(email, password, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var activeSessions = await db.UserSessions
            .Where(x => x.UserId == user.Id && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var activeSession in activeSessions)
        {
            activeSession.IsActive = false;
            activeSession.LogoutAt = now;
        }

        var histories = await db.LoginHistory
            .Where(x => x.UserId == user.Id && x.Status == "Active")
            .ToListAsync(cancellationToken);
        foreach (var history in histories)
        {
            history.Status = "Replaced";
            history.LogoutAt = now;
        }

        var sessionToken = Convert.ToHexString(Guid.NewGuid().ToByteArray());
        user.CurrentSessionToken = sessionToken;
        user.UpdatedAt = now;
        db.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            SessionToken = sessionToken,
            DeviceName = device.DeviceName,
            IpAddress = device.IpAddress,
            UserAgent = device.UserAgent,
            LoginAt = now
        });
        db.LoginHistory.Add(new LoginHistory
        {
            UserId = user.Id,
            DeviceName = device.DeviceName,
            IpAddress = device.IpAddress,
            UserAgent = device.UserAgent,
            LoginAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        return new AuthSession(user, sessionToken);
    }

    public async Task LogoutAsync(
        int userId,
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is not null && user.CurrentSessionToken == sessionToken)
        {
            user.CurrentSessionToken = null;
            user.UpdatedAt = now;
        }

        var session = await db.UserSessions.FirstOrDefaultAsync(
            x => x.UserId == userId && x.SessionToken == sessionToken,
            cancellationToken);
        if (session is not null)
        {
            session.IsActive = false;
            session.LogoutAt = now;
        }

        var history = await db.LoginHistory
            .Where(x => x.UserId == userId && x.Status == "Active")
            .OrderByDescending(x => x.LoginAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (history is not null)
        {
            history.Status = "LoggedOut";
            history.LogoutAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsSessionActiveAsync(
        int userId,
        string sessionToken,
        CancellationToken cancellationToken = default) =>
        db.Users.AnyAsync(
            x => x.Id == userId
                && x.IsActive
                && x.CurrentSessionToken == sessionToken,
            cancellationToken);

    public Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return db.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<UserSession>> GetSessionsAsync(CancellationToken cancellationToken = default) =>
        await db.UserSessions
            .Include(x => x.User)
            .AsNoTracking()
            .OrderByDescending(x => x.LoginAt)
            .ToListAsync(cancellationToken);
}

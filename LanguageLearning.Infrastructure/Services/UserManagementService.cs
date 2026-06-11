using LanguageLearning.Application.Features.Users;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Services;

public sealed class UserManagementService(LanguageLearningDbContext db) : IUserManagementService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<IReadOnlyList<UserDto>> GetAsync(
        string? role,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(x => x.Role == role);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query.OrderBy(x => x.FullName).Select(ToDto()).ToListAsync(cancellationToken);
    }

    public Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking().Where(x => x.Id == id).Select(ToDto()).FirstOrDefaultAsync(cancellationToken);

    public async Task<UserDto> CreateAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new User
        {
            FullName = command.FullName.Trim(),
            Email = email,
            PhoneNumber = command.PhoneNumber?.Trim(),
            Role = command.Role,
            LearningGoal = string.IsNullOrWhiteSpace(command.LearningGoal)
                ? "Giao tiep hang ngay"
                : command.LearningGoal.Trim(),
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, command.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task<UserDto?> UpdateAsync(
        int id,
        UpdateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.FullName = command.FullName.Trim();
        user.PhoneNumber = command.PhoneNumber?.Trim();
        user.Role = command.Role;
        user.IsActive = command.IsActive;
        user.LearningGoal = string.IsNullOrWhiteSpace(command.LearningGoal)
            ? user.LearningGoal
            : command.LearningGoal.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        if (!user.IsActive)
        {
            user.CurrentSessionToken = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.IsActive = false;
        user.CurrentSessionToken = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.UserSessions
            .Where(x => x.UserId == id && x.IsActive)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.IsActive, false)
                    .SetProperty(x => x.LogoutAt, DateTime.UtcNow),
                cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static System.Linq.Expressions.Expression<Func<User, UserDto>> ToDto() =>
        user => new UserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.Role,
            user.IsActive,
            user.LearningGoal,
            user.CreatedAt);

    private static UserDto Map(User user) =>
        new(
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.Role,
            user.IsActive,
            user.LearningGoal,
            user.CreatedAt);
}

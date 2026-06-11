using LanguageLearning.Domain;

namespace LanguageLearning.Application.Features.Users;

public sealed record UserDto(
    int Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    string LearningGoal,
    DateTime CreatedAt);

public sealed record CreateUserCommand(
    string FullName,
    string Email,
    string Password,
    string Role,
    string? PhoneNumber,
    string? LearningGoal);

public sealed record UpdateUserCommand(
    string FullName,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    string? LearningGoal);

public interface IUserManagementService
{
    Task<IReadOnlyList<UserDto>> GetAsync(string? role, bool? isActive, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateAsync(int id, UpdateUserCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

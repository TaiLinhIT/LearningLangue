using System.Net.Mail;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;

namespace LanguageLearning.WebUI.Services;

public interface ILearningAuthService
{
    Task<AuthResult> RegisterAsync(RegisterInput input, CancellationToken cancellationToken = default);

    Task<AuthResult> SignInAsync(SignInInput input, CancellationToken cancellationToken = default);

    Task<LearningUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public sealed record RegisterInput(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword,
    string LearningGoal);

public sealed record SignInInput(string Email, string Password);

public sealed record AuthResult(bool Succeeded, string Message, LearningUser? User = null)
{
    public static AuthResult Success(LearningUser user, string message) => new(true, message, user);

    public static AuthResult Failure(string message) => new(false, message);
}

public sealed class LearningUser
{
    public required int Id { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string LearningGoal { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class DatabaseLearningAuthService(IAuthService authService) : ILearningAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterInput input, CancellationToken cancellationToken = default)
    {
        var fullName = input.FullName.Trim();
        var email = input.Email.Trim();
        var learningGoal = string.IsNullOrWhiteSpace(input.LearningGoal)
            ? "Giao tiep hang ngay"
            : input.LearningGoal.Trim();

        if (fullName.Length < 2)
        {
            return AuthResult.Failure("Vui long nhap ho ten hop le.");
        }

        if (!IsValidEmail(email))
        {
            return AuthResult.Failure("Email khong hop le.");
        }

        if (input.Password.Length < 6)
        {
            return AuthResult.Failure("Mat khau can it nhat 6 ky tu.");
        }

        if (!string.Equals(input.Password, input.ConfirmPassword, StringComparison.Ordinal))
        {
            return AuthResult.Failure("Mat khau xac nhan khong khop.");
        }

        try
        {
            var user = await authService.RegisterAsync(fullName, email, input.Password, learningGoal, cancellationToken);
            return AuthResult.Success(ToLearningUser(user), "Dang ky thanh cong. Chao mung ban den voi LinguaFlow!");
        }
        catch (InvalidOperationException ex)
        {
            return AuthResult.Failure(ex.Message);
        }
    }

    public async Task<AuthResult> SignInAsync(SignInInput input, CancellationToken cancellationToken = default)
    {
        var email = input.Email.Trim();

        if (!IsValidEmail(email))
        {
            return AuthResult.Failure("Email khong hop le.");
        }

        var user = await authService.ValidateUserAsync(email, input.Password, cancellationToken);
        return user is null
            ? AuthResult.Failure("Email hoac mat khau khong dung.")
            : AuthResult.Success(ToLearningUser(user), "Dang nhap thanh cong.");
    }

    public async Task<LearningUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (!IsValidEmail(email.Trim()))
        {
            return null;
        }

        var user = await authService.FindByEmailAsync(email, cancellationToken);
        return user is null ? null : ToLearningUser(user);
    }

    private static LearningUser ToLearningUser(User user)
    {
        return new LearningUser
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            LearningGoal = user.LearningGoal,
            CreatedAt = user.CreatedAt
        };
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            return new MailAddress(email).Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

using System.Net.Mail;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;

namespace LanguageLearning.WebUI.Services;

public interface ILearningAuthService
{
    Task<AuthResult> RegisterAsync(RegisterInput input);
    Task<AuthResult> SignInAsync(SignInInput input);
    Task<bool> ExistsAsync(string email);
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

public sealed record LearningUser(
    int Id,
    string FullName,
    string Email,
    string Role,
    string SessionToken,
    string LearningGoal,
    DateTimeOffset CreatedAt);
public sealed class LearningUser
{
    public required int Id { get; init; }

public sealed class DatabaseLearningAuthService(
    IAuthService authService,
    IHttpContextAccessor httpContextAccessor) : ILearningAuthService
    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string LearningGoal { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class DatabaseLearningAuthService(IAuthService authService) : ILearningAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterInput input)
    public async Task<AuthResult> RegisterAsync(RegisterInput input, CancellationToken cancellationToken = default)
    {
        var validation = Validate(input);
        if (validation is not null)
        {
            return AuthResult.Failure(validation);
        }

        try
        {
            await authService.RegisterAsync(input.FullName.Trim(), input.Email.Trim(), input.Password);
            return await LoginAsync(input.Email, input.Password, input.LearningGoal, "Dang ky thanh cong.");
        }
        catch (InvalidOperationException ex)
        var fullName = input.FullName.Trim();
        var email = input.Email.Trim();
        var learningGoal = string.IsNullOrWhiteSpace(input.LearningGoal)
            ? "Giao tiep hang ngay"
            : input.LearningGoal.Trim();

        if (fullName.Length < 2)
        {
            return AuthResult.Failure(ex.Message);
            return AuthResult.Failure("Vui long nhap ho ten hop le.");
        }
    }

    public Task<AuthResult> SignInAsync(SignInInput input) =>
        LoginAsync(input.Email, input.Password, "Giao tiep hang ngay", "Dang nhap thanh cong.");

    public async Task<bool> ExistsAsync(string email) =>
        await authService.UserExistsAsync(email);

    private async Task<AuthResult> LoginAsync(
        string email,
        string password,
        string learningGoal,
        string successMessage)
    {
        if (!IsValidEmail(email))
        {
            return AuthResult.Failure("Email khong hop le.");
        }

        var context = httpContextAccessor.HttpContext;
        var userAgent = context?.Request.Headers.UserAgent.ToString();
        var device = new DeviceInfo(
            GetDeviceName(userAgent),
            context?.Connection.RemoteIpAddress?.ToString(),
            userAgent);
        var session = await authService.LoginAsync(email, password, device);
        if (session is null)
        {
            return AuthResult.Failure("Email hoac mat khau khong dung.");
            return AuthResult.Failure("Mat khau can it nhat 6 ky tu.");
        }

        if (!string.Equals(input.Password, input.ConfirmPassword, StringComparison.Ordinal))
        {
            return AuthResult.Failure("Mat khau xac nhan khong khop.");
        }

        return AuthResult.Success(
            new LearningUser(
                session.User.Id,
                session.User.FullName,
                session.User.Email,
                session.User.Role,
                session.SessionToken,
                learningGoal,
                session.User.CreatedAt),
            successMessage);
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

    private static string? Validate(RegisterInput input)
    public async Task<AuthResult> SignInAsync(SignInInput input, CancellationToken cancellationToken = default)
    {
        if (input.FullName.Trim().Length < 2)
        {
            return "Vui long nhap ho ten hop le.";
        }
            return AuthResult.Failure("Email khong hop le.");
        }

        var user = await authService.ValidateUserAsync(email, input.Password, cancellationToken);
        return user is null
            ? AuthResult.Failure("Email hoac mat khau khong dung.")
            : AuthResult.Success(ToLearningUser(user), "Dang nhap thanh cong.");
    }

        if (!IsValidEmail(input.Email))
    public async Task<LearningUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (!IsValidEmail(email.Trim()))
        {
            return "Email khong hop le.";
            return null;
        }

        if (input.Password.Length < 8)
        {
            return "Mat khau can it nhat 8 ky tu.";
        }
        var user = await authService.FindByEmailAsync(email, cancellationToken);
        return user is null ? null : ToLearningUser(user);
    }

        return input.Password != input.ConfirmPassword
            ? "Mat khau xac nhan khong khop."
            : null;
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

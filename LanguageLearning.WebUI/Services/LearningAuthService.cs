using System.Net.Mail;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;

namespace LanguageLearning.WebUI.Services;

public interface ILearningAuthService
{
    Task<AuthResult> RegisterAsync(RegisterInput input);
    Task<AuthResult> RegisterAsync(RegisterInput input, CancellationToken cancellationToken = default);
    Task<AuthResult> SignInAsync(SignInInput input);
    Task<AuthResult> SignInAsync(SignInInput input, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string email);
    Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default);
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
    DateTime CreatedAt);

public sealed class DatabaseLearningAuthService(
    IAuthService authService,
    IHttpContextAccessor httpContextAccessor) : ILearningAuthService
{
    private const string DefaultLearningGoal = "Giao tiep hang ngay";

    public Task<AuthResult> RegisterAsync(RegisterInput input) =>
        RegisterAsync(input, CancellationToken.None);

    public async Task<AuthResult> RegisterAsync(
        RegisterInput input,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(input);
        if (validation is not null)
        {
            return AuthResult.Failure(validation);
        }

        var fullName = input.FullName.Trim();
        var email = input.Email.Trim();
        var learningGoal = NormalizeLearningGoal(input.LearningGoal);

        try
        {
            await authService.RegisterAsync(
                fullName,
                email,
                input.Password,
                learningGoal,
                cancellationToken);

            return await LoginAsync(
                email,
                input.Password,
                "Dang ky thanh cong. Chao mung ban den voi LinguaFlow!",
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return AuthResult.Failure(ex.Message);
        }
    }

    public Task<AuthResult> SignInAsync(SignInInput input) =>
        SignInAsync(input, CancellationToken.None);

    public Task<AuthResult> SignInAsync(
        SignInInput input,
        CancellationToken cancellationToken = default) =>
        LoginAsync(input.Email, input.Password, "Dang nhap thanh cong.", cancellationToken);

    public Task<bool> ExistsAsync(string email) =>
        ExistsAsync(email, CancellationToken.None);

    public async Task<bool> ExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return IsValidEmail(email.Trim())
            && await authService.UserExistsAsync(email, cancellationToken);
    }

    public async Task<LearningUser?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidEmail(email.Trim()))
        {
            return null;
        }

        var user = await authService.FindByEmailAsync(email, cancellationToken);
        return user is null ? null : ToLearningUser(user, string.Empty);
    }

    private async Task<AuthResult> LoginAsync(
        string email,
        string password,
        string successMessage,
        CancellationToken cancellationToken)
    {
        if (!IsValidEmail(email.Trim()))
        {
            return AuthResult.Failure("Email khong hop le.");
        }

        if (password.Length < 6)
        {
            return AuthResult.Failure("Mat khau can it nhat 6 ky tu.");
        }

        var session = await authService.LoginAsync(
            email,
            password,
            CreateDeviceInfo(),
            cancellationToken);
        return session is null
            ? AuthResult.Failure("Email hoac mat khau khong dung.")
            : AuthResult.Success(ToLearningUser(session.User, session.SessionToken), successMessage);
    }

    private DeviceInfo CreateDeviceInfo()
    {
        var context = httpContextAccessor.HttpContext;
        var userAgent = context?.Request.Headers.UserAgent.ToString();
        return new DeviceInfo(
            GetDeviceName(userAgent),
            context?.Connection.RemoteIpAddress?.ToString(),
            userAgent);
    }

    private static string? Validate(RegisterInput input)
    {
        if (input.FullName.Trim().Length < 2)
        {
            return "Vui long nhap ho ten hop le.";
        }

        if (!IsValidEmail(input.Email.Trim()))
        {
            return "Email khong hop le.";
        }

        if (input.Password.Length < 6)
        {
            return "Mat khau can it nhat 6 ky tu.";
        }

        return !string.Equals(input.Password, input.ConfirmPassword, StringComparison.Ordinal)
            ? "Mat khau xac nhan khong khop."
            : null;
    }

    private static LearningUser ToLearningUser(User user, string sessionToken)
    {
        return new LearningUser(
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            sessionToken,
            NormalizeLearningGoal(user.LearningGoal),
            user.CreatedAt);
    }

    private static string NormalizeLearningGoal(string learningGoal) =>
        string.IsNullOrWhiteSpace(learningGoal) ? DefaultLearningGoal : learningGoal.Trim();

    private static string GetDeviceName(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Trinh duyet";
        }

        if (userAgent.Contains("Edg", StringComparison.OrdinalIgnoreCase))
        {
            return "Microsoft Edge";
        }

        if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
        {
            return "Google Chrome";
        }

        if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
        {
            return "Firefox";
        }

        if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase))
        {
            return "Safari";
        }

        return "Trinh duyet";
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

using System.Net.Mail;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;

namespace LanguageLearning.WebUI.Services;

public interface ILearningAuthService
{
    Task<AuthResult> RegisterAsync(RegisterInput input);
    Task<AuthResult> SignInAsync(SignInInput input);
    Task<bool> ExistsAsync(string email);
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

public sealed class DatabaseLearningAuthService(
    IAuthService authService,
    IHttpContextAccessor httpContextAccessor) : ILearningAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterInput input)
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
        {
            return AuthResult.Failure(ex.Message);
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
    }

    private static string? Validate(RegisterInput input)
    {
        if (input.FullName.Trim().Length < 2)
        {
            return "Vui long nhap ho ten hop le.";
        }

        if (!IsValidEmail(input.Email))
        {
            return "Email khong hop le.";
        }

        if (input.Password.Length < 8)
        {
            return "Mat khau can it nhat 8 ky tu.";
        }

        return input.Password != input.ConfirmPassword
            ? "Mat khau xac nhan khong khop."
            : null;
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

    private static string GetDeviceName(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Unknown device";
        }

        var browser = userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase) ? "Edge"
            : userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) ? "Chrome"
            : userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase) ? "Firefox"
            : userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase) ? "Safari"
            : "Browser";
        var os = userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) ? "Windows"
            : userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ? "Android"
            : userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ? "iPhone"
            : userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase) ? "macOS"
            : "Device";
        return $"{browser} on {os}";
    }
}

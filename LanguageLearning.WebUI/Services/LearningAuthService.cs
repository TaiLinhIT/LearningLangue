using System.Collections.Concurrent;
using System.Net.Mail;
using System.Security.Cryptography;

namespace LanguageLearning.WebUI.Services;

public interface ILearningAuthService
{
    Task<AuthResult> RegisterAsync(RegisterInput input);

    Task<AuthResult> SignInAsync(SignInInput input);

    LearningUser? FindByEmail(string email);
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
    public required Guid Id { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string PasswordHash { get; init; }

    public required string LearningGoal { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastSignedInAt { get; set; }
}

public sealed class InMemoryLearningAuthService : ILearningAuthService
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 120_000;

    private readonly ConcurrentDictionary<string, LearningUser> _users = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryLearningAuthService()
    {
        var demo = new LearningUser
        {
            Id = Guid.Parse("5ee44aa1-70a3-44e4-910e-76d2a6f88d43"),
            FullName = "Nguyen Linh",
            Email = "linh@example.com",
            LearningGoal = "Giao tiep hang ngay",
            PasswordHash = HashPassword("Demo@123")
        };

        _users[NormalizeEmail(demo.Email)] = demo;
    }

    public Task<AuthResult> RegisterAsync(RegisterInput input)
    {
        var fullName = input.FullName.Trim();
        var email = input.Email.Trim();
        var learningGoal = string.IsNullOrWhiteSpace(input.LearningGoal)
            ? "Giao tiep hang ngay"
            : input.LearningGoal.Trim();

        if (fullName.Length < 2)
        {
            return Task.FromResult(AuthResult.Failure("Vui long nhap ho ten hop le."));
        }

        if (!IsValidEmail(email))
        {
            return Task.FromResult(AuthResult.Failure("Email khong hop le."));
        }

        if (input.Password.Length < 6)
        {
            return Task.FromResult(AuthResult.Failure("Mat khau can it nhat 6 ky tu."));
        }

        if (!string.Equals(input.Password, input.ConfirmPassword, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthResult.Failure("Mat khau xac nhan khong khop."));
        }

        var normalizedEmail = NormalizeEmail(email);
        var user = new LearningUser
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            LearningGoal = learningGoal,
            PasswordHash = HashPassword(input.Password),
            LastSignedInAt = DateTimeOffset.UtcNow
        };

        return Task.FromResult(_users.TryAdd(normalizedEmail, user)
            ? AuthResult.Success(user, "Dang ky thanh cong. Chao mung ban den voi LinguaFlow!")
            : AuthResult.Failure("Email nay da duoc dang ky."));
    }

    public Task<AuthResult> SignInAsync(SignInInput input)
    {
        var email = input.Email.Trim();

        if (!IsValidEmail(email))
        {
            return Task.FromResult(AuthResult.Failure("Email khong hop le."));
        }

        if (!_users.TryGetValue(NormalizeEmail(email), out var user)
            || !VerifyPassword(input.Password, user.PasswordHash))
        {
            return Task.FromResult(AuthResult.Failure("Email hoac mat khau khong dung."));
        }

        user.LastSignedInAt = DateTimeOffset.UtcNow;
        return Task.FromResult(AuthResult.Success(user, "Dang nhap thanh cong."));
    }

    public LearningUser? FindByEmail(string email)
    {
        return _users.TryGetValue(NormalizeEmail(email), out var user) ? user : null;
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

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static string HashPassword(string password)
    {
        Span<byte> salt = stackalloc byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt.ToArray(), Iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize);

        return $"v1.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.', 4);
        if (parts.Length != 4 || parts[0] != "v1" || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expectedKey = Convert.FromBase64String(parts[3]);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var actualKey = pbkdf2.GetBytes(expectedKey.Length);

        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}

namespace LanguageLearning.API.Features.Auth;

public sealed record LoginRequest(string Email, string Password, string? DeviceName);
public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    int UserId,
    string FullName,
    string Email,
    string Role,
    string LearningGoal);
public sealed record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? LearningGoal);
public sealed record ForgotPasswordRequest(string Email);

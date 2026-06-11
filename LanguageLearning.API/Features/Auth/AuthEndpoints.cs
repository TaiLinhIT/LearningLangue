using System.Security.Claims;
using LanguageLearning.API.Common;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;

namespace LanguageLearning.API.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync);
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapPost("/refresh-token", RefreshTokenAsync).RequireAuthorization();
        group.MapGet("/me", GetMe).RequireAuthorization();
        group.MapPost("/forgot-password", ForgotPasswordAsync);

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        IAuthService authService,
        IJwtTokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (!ApiValidation.IsEmail(request.Email) || request.Password.Length < 8)
        {
            return Results.BadRequest(ApiResponse<object>.Fail("Email hoac mat khau khong hop le."));
        }

        var session = await authService.LoginAsync(
            request.Email,
            request.Password,
            new DeviceInfo(
                string.IsNullOrWhiteSpace(request.DeviceName) ? "API client" : request.DeviceName.Trim(),
                httpContext.Connection.RemoteIpAddress?.ToString(),
                httpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        var token = tokenService.Create(session.User, session.SessionToken);
        return Results.Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse(
            token.AccessToken,
            token.ExpiresAt,
            session.User.Id,
            session.User.FullName,
            session.User.Email,
            session.User.Role,
            session.User.LearningGoal), "Dang nhap thanh cong"));
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        if (request.FullName.Trim().Length < 2
            || !ApiValidation.IsEmail(request.Email)
            || request.Password.Length < 8
            || request.Password != request.ConfirmPassword)
        {
            return Results.BadRequest(ApiResponse<object>.Fail("Du lieu dang ky khong hop le."));
        }

        try
        {
            var user = await authService.RegisterAsync(
                request.FullName,
                request.Email,
                request.Password,
                string.IsNullOrWhiteSpace(request.LearningGoal)
                    ? "Giao tiep hang ngay"
                    : request.LearningGoal.Trim(),
                cancellationToken);
            return Results.Created(
                $"/api/users/{user.Id}",
                ApiResponse<object>.Ok(new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.LearningGoal
                }, "Tao tai khoan thanh cong"));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    private static async Task<IResult> LogoutAsync(
        ClaimsPrincipal principal,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(
            principal.GetUserId(),
            principal.GetSessionToken(),
            cancellationToken);
        return Results.Ok(ApiResponse<object>.Ok(new { }, "Dang xuat thanh cong"));
    }

    private static async Task<IResult> RefreshTokenAsync(
        ClaimsPrincipal principal,
        IAuthService authService,
        IJwtTokenService tokenService,
        CancellationToken cancellationToken)
    {
        var user = await authService.FindByEmailAsync(
            principal.FindFirstValue(ClaimTypes.Email)!,
            cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var token = tokenService.Create(user, principal.GetSessionToken());
        return Results.Ok(ApiResponse<AccessTokenResult>.Ok(token));
    }

    private static IResult GetMe(ClaimsPrincipal principal) =>
        Results.Ok(ApiResponse<object>.Ok(new
        {
            Id = principal.GetUserId(),
            Name = principal.Identity?.Name,
            Email = principal.FindFirstValue(ClaimTypes.Email),
            Role = principal.GetRole()
        }));

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        if (ApiValidation.IsEmail(request.Email))
        {
            await authService.FindByEmailAsync(request.Email, cancellationToken);
        }

        return Results.Ok(ApiResponse<object>.Ok(
            new { },
            "Neu email ton tai, he thong se gui huong dan khoi phuc mat khau."));
    }
}

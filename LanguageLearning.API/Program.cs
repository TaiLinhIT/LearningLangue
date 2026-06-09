using LanguageLearning.Application.Abstractions;
using LanguageLearning.Infrastructure;
using LanguageLearning.Infrastructure.Data;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Name = "LanguageLearning API",
    Version = "2.0",
    Endpoints = new[] { "/api/languages", "/api/courses", "/api/lessons/{id}", "/api/progress/{userId}", "/api/auth/login", "/api/auth/register" }
}));

app.MapGet("/api/languages", async (ILearningCatalogService catalog) => await catalog.GetLanguagesAsync());
app.MapGet("/api/courses", async (ILearningCatalogService catalog, bool includeDrafts) => await catalog.GetCoursesAsync(includeDrafts));
app.MapGet("/api/courses/{id:int}", async (ILearningCatalogService catalog, int id) =>
    await catalog.GetCourseAsync(id) is { } course ? Results.Ok(course) : Results.NotFound());
app.MapGet("/api/lessons/{id:int}", async (ILearningCatalogService catalog, int id) =>
    await catalog.GetLessonAsync(id) is { } lesson ? Results.Ok(lesson) : Results.NotFound());
app.MapGet("/api/progress/{userId:int}", async (ILearningCatalogService catalog, int userId) => await catalog.GetProgressAsync(userId));
app.MapGet("/api/mistakes/{userId:int}", async (ILearningCatalogService catalog, int userId) => await catalog.GetMistakesAsync(userId));

app.MapPost("/api/auth/login", async (ApiLoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    if (!IsValidEmail(request.Email))
    {
        return Results.BadRequest(new ApiAuthMessage("Email khong hop le."));
    }

    var user = await authService.ValidateUserAsync(request.Email, request.Password, cancellationToken);
    return user is null
        ? Results.Unauthorized()
        : Results.Ok(ToAuthResponse(user));
});

app.MapPost("/api/auth/register", async (ApiRegisterRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    var fullName = request.FullName.Trim();
    if (fullName.Length < 2)
    {
        return Results.BadRequest(new ApiAuthMessage("Vui long nhap ho ten hop le."));
    }

    if (!IsValidEmail(request.Email))
    {
        return Results.BadRequest(new ApiAuthMessage("Email khong hop le."));
    }

    if (request.Password.Length < 6)
    {
        return Results.BadRequest(new ApiAuthMessage("Mat khau can it nhat 6 ky tu."));
    }

    if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
    {
        return Results.BadRequest(new ApiAuthMessage("Mat khau xac nhan khong khop."));
    }

    try
    {
        var user = await authService.RegisterAsync(
            fullName,
            request.Email,
            request.Password,
            request.LearningGoal,
            cancellationToken);

        return Results.Created($"/api/users/{user.Id}", ToAuthResponse(user));
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new ApiAuthMessage(ex.Message));
    }
});

app.MapPost("/api/auth/forgot-password", async (ApiForgotPasswordRequest request, IAuthService authService, CancellationToken cancellationToken) =>
{
    if (IsValidEmail(request.Email))
    {
        await authService.FindByEmailAsync(request.Email, cancellationToken);
    }

    return Results.Ok(new ApiAuthMessage("Neu email ton tai, chung toi se gui huong dan khoi phuc mat khau."));
});

app.MapPost("/api/auth/external/{provider}", (string provider) =>
{
    var safeProvider = provider.ToLowerInvariant() switch
    {
        "google" => "Google",
        "facebook" => "Facebook",
        "github" => "Github",
        _ => "nha cung cap nay"
    };

    return Results.Problem(
        title: "External auth is not configured",
        detail: $"Dang nhap voi {safeProvider} chua duoc cau hinh OAuth client id/secret.",
        statusCode: StatusCodes.Status501NotImplemented);
});

await LanguageLearningDbInitializer.InitializeAsync(app.Services);
app.Run();

static bool IsValidEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email))
    {
        return false;
    }

    try
    {
        return new MailAddress(email.Trim()).Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase);
    }
    catch (FormatException)
    {
        return false;
    }
}

static ApiAuthResponse ToAuthResponse(LanguageLearning.Domain.User user) =>
    new(user.Id, user.FullName, user.Email, user.Role, user.LearningGoal);

public sealed record ApiLoginRequest(string Email, string Password);

public sealed record ApiRegisterRequest(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword,
    string LearningGoal);

public sealed record ApiForgotPasswordRequest(string Email);

public sealed record ApiAuthResponse(int Id, string FullName, string Email, string Role, string LearningGoal);

public sealed record ApiAuthMessage(string Message);

using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure;
using LanguageLearning.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = []
    });
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "LinguaFlow-development-key-change-this-before-production-2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LanguageLearning.API";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var sessionToken = context.Principal?.FindFirstValue("session_token");
                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                if (!int.TryParse(userIdValue, out var userId)
                    || string.IsNullOrWhiteSpace(sessionToken)
                    || !await authService.IsSessionActiveAsync(
                        userId,
                        sessionToken,
                        context.HttpContext.RequestAborted))
                {
                    context.Fail("Tai khoan da dang nhap o thiet bi khac");
                }
            },
            OnChallenge = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ApiResponse<object>(
                    false,
                    "Tai khoan da dang nhap o thiet bi khac",
                    null));
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    Name = "LanguageLearning API",
    Version = "2.1",
    Authentication = "JWT bearer with one active session per user"
}));

var auth = app.MapGroup("/api/auth");
auth.MapPost("/login", async (
    LoginRequest request,
    HttpContext httpContext,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    if (!IsValidEmail(request.Email))
    {
        return Results.BadRequest(new ApiResponse<object>(false, "Email khong hop le", null));
    }

    if (request.Password.Length < 6)
    {
        return Results.BadRequest(new ApiResponse<object>(false, "Mat khau can it nhat 6 ky tu", null));
    }

    var device = new DeviceInfo(
        string.IsNullOrWhiteSpace(request.DeviceName) ? "API client" : request.DeviceName,
        httpContext.Connection.RemoteIpAddress?.ToString(),
        httpContext.Request.Headers.UserAgent.ToString());
    var session = await authService.LoginAsync(request.Email, request.Password, device, cancellationToken);
    if (session is null)
    {
        return Results.Unauthorized();
    }

    var expiresAt = DateTime.UtcNow.AddHours(8);
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, session.User.Id.ToString()),
        new Claim(ClaimTypes.Name, session.User.FullName),
        new Claim(ClaimTypes.Email, session.User.Email),
        new Claim(ClaimTypes.Role, session.User.Role),
        new Claim("session_token", session.SessionToken)
    };
    var token = new JwtSecurityToken(
        jwtIssuer,
        jwtIssuer,
        claims,
        expires: expiresAt,
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
    var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new ApiResponse<LoginResponse>(
        true,
        "Dang nhap thanh cong",
        new LoginResponse(
            accessToken,
            expiresAt,
            session.User.Id,
            session.User.FullName,
            session.User.Email,
            session.User.Role,
            session.User.LearningGoal)));
});
auth.MapPost("/register", async (
    RegisterRequest request,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var fullName = request.FullName.Trim();
    if (fullName.Length < 2)
    {
        return Results.BadRequest(new ApiResponse<object>(
            false,
            "Vui long nhap ho ten hop le",
            null));
    }

    if (!IsValidEmail(request.Email))
    {
        return Results.BadRequest(new ApiResponse<object>(
            false,
            "Email khong hop le",
            null));
    }

    if (request.Password.Length < 6)
    {
        return Results.BadRequest(new ApiResponse<object>(
            false,
            "Mat khau can it nhat 6 ky tu",
            null));
    }

    if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
    {
        return Results.BadRequest(new ApiResponse<object>(
            false,
            "Mat khau xac nhan khong khop",
            null));
    }

    try
    {
        var user = await authService.RegisterAsync(
            fullName,
            request.Email,
            request.Password,
            NormalizeLearningGoal(request.LearningGoal),
            cancellationToken);
        return Results.Created(
            $"/api/users/{user.Id}",
            new ApiResponse<object>(
                true,
                "Tao tai khoan thanh cong",
                new { user.Id, user.FullName, user.Email, user.Role, user.LearningGoal }));
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new ApiResponse<object>(false, ex.Message, null));
    }
});
auth.MapPost("/logout", async (
    ClaimsPrincipal principal,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var sessionToken = principal.FindFirstValue("session_token")!;
    await authService.LogoutAsync(userId, sessionToken, cancellationToken);
    return Results.Ok(new ApiResponse<object>(true, "Dang xuat thanh cong", null));
}).RequireAuthorization();
auth.MapGet("/me", (ClaimsPrincipal principal) => Results.Ok(new ApiResponse<object>(
    true,
    "OK",
    new
    {
        Id = principal.FindFirstValue(ClaimTypes.NameIdentifier),
        Name = principal.Identity?.Name,
        Email = principal.FindFirstValue(ClaimTypes.Email),
        Role = principal.FindFirstValue(ClaimTypes.Role)
    }))).RequireAuthorization();

auth.MapPost("/forgot-password", async (
    ForgotPasswordRequest request,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    if (IsValidEmail(request.Email))
    {
        await authService.FindByEmailAsync(request.Email, cancellationToken);
    }

    return Results.Ok(new ApiResponse<object>(
        true,
        "Neu email ton tai, chung toi se gui huong dan khoi phuc mat khau",
        null));
});

auth.MapPost("/external/{provider}", (string provider) =>
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

app.MapGet("/api/languages", async (ILearningCatalogService catalog) =>
    Results.Ok(new ApiResponse<object>(true, "OK", await catalog.GetLanguagesAsync())));
app.MapGet("/api/courses", async (ILearningCatalogService catalog, bool includeDrafts) =>
    Results.Ok(new ApiResponse<object>(true, "OK", await catalog.GetCoursesAsync(includeDrafts))));
app.MapGet("/api/courses/{id:int}", async (ILearningCatalogService catalog, int id) =>
    await catalog.GetCourseAsync(id) is { } course
        ? Results.Ok(new ApiResponse<object>(true, "OK", course))
        : Results.NotFound(new ApiResponse<object>(false, "Khong tim thay khoa hoc", null)));
app.MapGet("/api/lessons/{id:int}", async (ILearningCatalogService catalog, int id) =>
    await catalog.GetLessonAsync(id) is { } lesson
        ? Results.Ok(new ApiResponse<object>(true, "OK", lesson))
        : Results.NotFound(new ApiResponse<object>(false, "Khong tim thay bai hoc", null)));

app.MapGet("/api/progress/me", async (
    ClaimsPrincipal principal,
    ILearningCatalogService catalog) =>
{
    var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    return Results.Ok(new ApiResponse<object>(true, "OK", await catalog.GetProgressAsync(userId)));
}).RequireAuthorization();

app.MapPost("/api/quizzes/{lessonId:int}/submit", async (
    int lessonId,
    QuizAnswersRequest request,
    ClaimsPrincipal principal,
    ILearningCatalogService catalog,
    CancellationToken cancellationToken) =>
{
    var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await catalog.SubmitQuizAsync(
        userId,
        new QuizSubmission(lessonId, request.Answers),
        cancellationToken);
    return Results.Ok(new ApiResponse<QuizResult>(true, "Da cham diem", result));
}).RequireAuthorization();

app.MapPost("/api/sentence-practice/submit", async (
    SentencePracticeRequest request,
    ClaimsPrincipal principal,
    ISentencePracticeService service,
    CancellationToken cancellationToken) =>
{
    var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.SubmitAsync(
        new SentenceScoringRequest(
            userId,
            request.LessonStepId,
            request.Sentence,
            request.GrammarStructure,
            request.Vocabulary),
        cancellationToken);
    return Results.Ok(new ApiResponse<AIScoringResult>(true, "AI da cham diem", result));
}).RequireAuthorization();

app.MapGet("/api/admin/sessions", async (
    IAuthService authService,
    CancellationToken cancellationToken) =>
    Results.Ok(new ApiResponse<object>(true, "OK", await authService.GetSessionsAsync(cancellationToken))))
    .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

await LanguageLearningDbInitializer.InitializeAsync(app.Services);
app.Run();

static bool IsValidEmail(string email)
{
    try
    {
        return new MailAddress(email.Trim()).Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase);
    }
    catch (FormatException)
    {
        return false;
    }
}

static string NormalizeLearningGoal(string? learningGoal) =>
    string.IsNullOrWhiteSpace(learningGoal) ? "Giao tiep hang ngay" : learningGoal.Trim();

public sealed record ApiResponse<T>(bool Success, string Message, T? Data);
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
public sealed record QuizAnswersRequest(IReadOnlyDictionary<int, string> Answers);
public sealed record SentencePracticeRequest(
    int LessonStepId,
    string Sentence,
    string GrammarStructure,
    IReadOnlyList<string> Vocabulary);

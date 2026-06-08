using System.Security.Claims;
using AntDesign;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Infrastructure;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.WebUI.Components;
using LanguageLearning.WebUI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMudServices();
builder.Services.AddSingleton<ILearningAuthService, InMemoryLearningAuthService>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth";
        options.LogoutPath = "/auth/sign-out";
        options.AccessDeniedPath = "/auth";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/auth/sign-in", async (HttpContext httpContext, ILearningAuthService authService) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var result = await authService.SignInAsync(new SignInInput(
        form["email"].ToString(),
        form["password"].ToString()));

    if (!result.Succeeded || result.User is null)
    {
        return RedirectToAuth("login", "error", result.Message);
    }

    await SignInUserAsync(httpContext, result.User, form.ContainsKey("remember"));
    return Results.Redirect("/account?status=signed-in");
}).DisableAntiforgery();

app.MapPost("/auth/register", async (HttpContext httpContext, ILearningAuthService authService) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var result = await authService.RegisterAsync(new RegisterInput(
        form["fullName"].ToString(),
        form["email"].ToString(),
        form["password"].ToString(),
        form["confirmPassword"].ToString(),
        form["learningGoal"].ToString()));

    if (!result.Succeeded || result.User is null)
    {
        return RedirectToAuth("register", "error", result.Message);
    }

    await SignInUserAsync(httpContext, result.User, rememberMe: true);
    return Results.Redirect("/account?status=registered");
}).DisableAntiforgery();

app.MapPost("/auth/forgot-password", async (HttpContext httpContext, ILearningAuthService authService) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var message = authService.FindByEmail(email) is null
        ? "Neu email ton tai, chung toi se gui huong dan khoi phuc mat khau."
        : "Da tao yeu cau khoi phuc mat khau cho tai khoan nay.";

    return RedirectToAuth("login", "success", message);
}).DisableAntiforgery();

app.MapPost("/auth/sign-out", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return RedirectToAuth("login", "success", "Ban da dang xuat.");
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await LanguageLearningDbInitializer.InitializeAsync(app.Services);
app.Run();

static async Task SignInUserAsync(HttpContext httpContext, LearningUser user, bool rememberMe)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.FullName),
        new(ClaimTypes.Email, user.Email),
        new("learning_goal", user.LearningGoal),
        new("created_at", user.CreatedAt.ToString("O"))
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    var properties = new AuthenticationProperties
    {
        IsPersistent = rememberMe,
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(rememberMe ? 14 : 1)
    };

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
}

static IResult RedirectToAuth(string mode, string status, string message)
{
    var url = $"/auth?mode={Uri.EscapeDataString(mode)}&status={Uri.EscapeDataString(status)}&message={Uri.EscapeDataString(message)}";
    return Results.Redirect(url);
}

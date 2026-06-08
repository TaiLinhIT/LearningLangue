using System.Security.Claims;
using AntDesign;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Infrastructure;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.WebUI.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntDesign();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("LearnerOnly", policy => policy.RequireRole("Learner", "Admin"));
});
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/account/login", async (
    HttpContext httpContext,
    IAuthService authService,
    string email,
    string password,
    string? returnUrl) =>
{
    var user = await authService.ValidateUserAsync(email, password);
    if (user is null)
    {
        return Results.Redirect($"/login?error=1&email={Uri.EscapeDataString(email)}");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.FullName),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Role, user.Role)
    };

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

    return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl)
        ? user.Role == "Admin" ? "/admin" : "/dashboard"
        : returnUrl);
}).DisableAntiforgery();

app.MapPost("/account/register", async (
    HttpContext httpContext,
    IAuthService authService,
    string fullName,
    string email,
    string password) =>
{
    try
    {
        var user = await authService.RegisterAsync(fullName, email, password);
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            ], CookieAuthenticationDefaults.AuthenticationScheme)));

        return Results.Redirect("/learning-goal");
    }
    catch
    {
        return Results.Redirect($"/register?error=1&email={Uri.EscapeDataString(email)}");
    }
}).DisableAntiforgery();

app.MapPost("/account/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await LanguageLearningDbInitializer.InitializeAsync(app.Services);
app.Run();

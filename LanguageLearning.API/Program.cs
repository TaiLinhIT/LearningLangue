using System.Security.Claims;
using System.Text;
using LanguageLearning.API.Common;
using LanguageLearning.API.Features.Admin;
using LanguageLearning.API.Features.Auth;
using LanguageLearning.API.Features.Classes;
using LanguageLearning.API.Features.Content;
using LanguageLearning.API.Features.Courses;
using LanguageLearning.API.Features.Engagement;
using LanguageLearning.API.Features.Learning;
using LanguageLearning.API.Features.Users;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Infrastructure;
using LanguageLearning.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LanguageLearning API",
        Version = "v1",
        Description = "LMS API for students, teachers, receptionists and administrators."
    });
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

var jwtSettings = new JwtSettings
{
    Key = builder.Configuration["Jwt:Key"]
        ?? "LinguaFlow-development-key-change-this-before-production-2026",
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "LanguageLearning.API",
    AccessTokenHours = builder.Configuration.GetValue("Jwt:AccessTokenHours", 8)
};
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
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
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Issuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
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
                await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                    "Phien dang nhap khong hop le hoac tai khoan da dang nhap o thiet bi khac."));
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

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(ApiResponse<object>.Ok(new
{
    Name = "LanguageLearning API",
    Version = "3.0",
    Architecture = "Feature folders + application contracts + infrastructure services",
    Swagger = "/swagger"
})));
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapCourseEndpoints();
app.MapContentEndpoints();
app.MapLearningEndpoints();
app.MapClassEndpoints();
app.MapEngagementEndpoints();
app.MapAdminEndpoints();

await LanguageLearningDbInitializer.InitializeAsync(app.Services);
app.Run();

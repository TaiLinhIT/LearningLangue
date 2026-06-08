using LanguageLearning.Application.Abstractions;
using LanguageLearning.Infrastructure;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Name = "LanguageLearning API",
    Version = "2.0",
    Endpoints = new[] { "/api/languages", "/api/courses", "/api/lessons/{id}", "/api/progress/{userId}" }
}));

app.MapGet("/api/languages", async (ILearningCatalogService catalog) => await catalog.GetLanguagesAsync());
app.MapGet("/api/courses", async (ILearningCatalogService catalog, bool includeDrafts) => await catalog.GetCoursesAsync(includeDrafts));
app.MapGet("/api/courses/{id:int}", async (ILearningCatalogService catalog, int id) =>
    await catalog.GetCourseAsync(id) is { } course ? Results.Ok(course) : Results.NotFound());
app.MapGet("/api/lessons/{id:int}", async (ILearningCatalogService catalog, int id) =>
    await catalog.GetLessonAsync(id) is { } lesson ? Results.Ok(lesson) : Results.NotFound());
app.MapGet("/api/progress/{userId:int}", async (ILearningCatalogService catalog, int userId) => await catalog.GetProgressAsync(userId));
app.MapGet("/api/mistakes/{userId:int}", async (ILearningCatalogService catalog, int userId) => await catalog.GetMistakesAsync(userId));

await LanguageLearningDbInitializer.InitializeAsync(app.Services);
app.Run();

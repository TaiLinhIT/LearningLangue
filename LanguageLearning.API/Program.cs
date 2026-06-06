using LanguageLearning.Application.Abstractions;
using LanguageLearning.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddPersistence();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGet("/", () => Results.Ok(new
{
    Name = "LanguageLearning API",
    Version = "1.0",
    Endpoints = new[] { "/api/languages", "/api/lessons", "/api/vocabulary", "/api/progress", "/api/pricing" }
}));

app.MapGet("/api/languages", (ILearningCatalogService catalog) => catalog.GetLanguages());
app.MapGet("/api/lessons", (ILearningCatalogService catalog, string? languageCode) => catalog.GetLessons(languageCode));
app.MapGet("/api/lessons/{id:int}", (ILearningCatalogService catalog, int id) =>
    catalog.GetLesson(id) is { } lesson ? Results.Ok(lesson) : Results.NotFound());
app.MapGet("/api/vocabulary", (ILearningCatalogService catalog, string? query) => catalog.GetVocabulary(query));
app.MapGet("/api/placement-test", (ILearningCatalogService catalog) => catalog.GetPlacementQuestions());
app.MapGet("/api/progress", (ILearningCatalogService catalog) => catalog.GetProgress());
app.MapGet("/api/pricing", (ILearningCatalogService catalog) => catalog.GetPricingPlans());
app.MapGet("/api/admin/metrics", (ILearningCatalogService catalog) => catalog.GetAdminMetrics());

app.Run();

using LanguageLearning.Application.Abstractions;
using LanguageLearning.Persistence.Data;
using LanguageLearning.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LanguageLearning API",
        Version = "v1",
        Description = "Learning catalog, lessons, vocabulary, progress, pricing and database health endpoints."
    });
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5018", "https://localhost:7280")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "LanguageLearning API v1");
    options.DocumentTitle = "LanguageLearning API";
    options.RoutePrefix = "swagger";
});

app.UseCors();

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "LanguageLearning API" }))
    .WithName("Health")
    .WithTags("System");

var api = app.MapGroup("/api");

api.MapGet("/database/status", async (IServiceProvider services, IConfiguration configuration) =>
    {
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Results.Ok(new
            {
                Configured = false,
                CanConnect = false,
                Message = "Connection string 'Default' is missing."
            });
        }

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<LearningDbContext>();

        if (dbContext is null)
        {
            return Results.Ok(new
            {
                Configured = true,
                CanConnect = false,
                Message = "LearningDbContext is not registered."
            });
        }

        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync();
            var connection = dbContext.Database.GetDbConnection();

            return Results.Ok(new
            {
                Configured = true,
                CanConnect = canConnect,
                Provider = dbContext.Database.ProviderName,
                Server = connection.DataSource,
                Database = connection.Database
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Database connection failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    })
    .WithName("DatabaseStatus")
    .WithTags("Database");

api.MapPost("/database/initialize-demo", async (DatabaseSeeder seeder, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await seeder.EnsureCreatedAndSeedAsync(cancellationToken);
            return Results.Ok(new
            {
                result.DatabaseCreated,
                result.RowsInserted,
                Message = result.RowsInserted == 0
                    ? "Database is ready. Existing data was kept."
                    : "Database is ready and demo learning data was inserted."
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Database initialization failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    })
    .WithName("InitializeDemoDatabase")
    .WithTags("Database");

api.MapGet("/languages", (ILearningCatalogService catalog) => catalog.GetLanguages())
    .WithName("GetLanguages")
    .WithTags("Learning");
api.MapGet("/lessons", (ILearningCatalogService catalog, string? languageCode) => catalog.GetLessons(languageCode))
    .WithName("GetLessons")
    .WithTags("Learning");
api.MapGet("/lessons/{id:int}", (ILearningCatalogService catalog, int id) =>
        catalog.GetLesson(id) is { } lesson ? Results.Ok(lesson) : Results.NotFound())
    .WithName("GetLesson")
    .WithTags("Learning");
api.MapGet("/vocabulary", (ILearningCatalogService catalog, string? query) => catalog.GetVocabulary(query))
    .WithName("GetVocabulary")
    .WithTags("Learning");
api.MapGet("/placement-test", (ILearningCatalogService catalog) => catalog.GetPlacementQuestions())
    .WithName("GetPlacementTest")
    .WithTags("Learning");
api.MapGet("/progress", (ILearningCatalogService catalog) => catalog.GetProgress())
    .WithName("GetProgress")
    .WithTags("Learning");
api.MapGet("/pricing", (ILearningCatalogService catalog) => catalog.GetPricingPlans())
    .WithName("GetPricing")
    .WithTags("Learning");
api.MapGet("/admin/metrics", (ILearningCatalogService catalog) => catalog.GetAdminMetrics())
    .WithName("GetAdminMetrics")
    .WithTags("Admin");

app.Run();

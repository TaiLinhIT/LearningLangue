using LanguageLearning.Application.Abstractions;
using LanguageLearning.Infrastructure.Data;
using LanguageLearning.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=LanguageLearning;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False";

        services.AddDbContext<LanguageLearningDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ILearningCatalogService, LearningCatalogService>();
        services.AddScoped<IAdminLearningService, AdminLearningService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAIScoringService, MockAIScoringService>();
        services.AddScoped<ISentencePracticeService, SentencePracticeService>();
        services.AddScoped<ILearningExperienceService, LearningExperienceService>();

        return services;
    }
}

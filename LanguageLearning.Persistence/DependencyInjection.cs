using LanguageLearning.Application.Abstractions;
using LanguageLearning.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<DemoLearningCatalogService>();

        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<ILearningCatalogService>(provider =>
                provider.GetRequiredService<DemoLearningCatalogService>());

            return services;
        }

        services.AddDbContext<LearningDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<ILearningCatalogService, DatabaseLearningCatalogService>();

        return services;
    }
}

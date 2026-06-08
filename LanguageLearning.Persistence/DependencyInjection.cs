using LanguageLearning.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ILearningCatalogService, DemoLearningCatalogService>();
        return services;
    }
}

using LanguageLearning.API.Common;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;

namespace LanguageLearning.API.Features.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
        group.MapGet("/sessions", async (
            IAuthService authService,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<object>.Ok(
                await authService.GetSessionsAsync(cancellationToken))));
        return app;
    }
}

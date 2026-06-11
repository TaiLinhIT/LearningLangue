using LanguageLearning.API.Common;
using LanguageLearning.Application.Features.Users;
using LanguageLearning.Domain;

namespace LanguageLearning.API.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        group.MapGet("/", async (
            string? role,
            bool? isActive,
            IUserManagementService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<UserDto>>.Ok(
                await service.GetAsync(role, isActive, cancellationToken))));

        group.MapGet("/{id:int}", async (
            int id,
            IUserManagementService service,
            CancellationToken cancellationToken) =>
            await service.GetByIdAsync(id, cancellationToken) is { } user
                ? Results.Ok(ApiResponse<UserDto>.Ok(user))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay user.")));

        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", async (
            int id,
            IUserManagementService service,
            CancellationToken cancellationToken) =>
            await service.DeactivateAsync(id, cancellationToken)
                ? Results.Ok(ApiResponse<object>.Ok(new { }, "Da vo hieu hoa tai khoan."))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay user.")));

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateUserCommand command,
        IUserManagementService service,
        CancellationToken cancellationToken)
    {
        if (command.FullName.Trim().Length < 2
            || !ApiValidation.IsEmail(command.Email)
            || command.Password.Length < 8
            || !ApiValidation.IsRole(command.Role))
        {
            return Results.BadRequest(ApiResponse<object>.Fail("Du lieu user khong hop le."));
        }

        try
        {
            var user = await service.CreateAsync(command, cancellationToken);
            return Results.Created($"/api/users/{user.Id}", ApiResponse<UserDto>.Ok(user));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateUserCommand command,
        IUserManagementService service,
        CancellationToken cancellationToken)
    {
        if (command.FullName.Trim().Length < 2 || !ApiValidation.IsRole(command.Role))
        {
            return Results.BadRequest(ApiResponse<object>.Fail("Du lieu user khong hop le."));
        }

        return await service.UpdateAsync(id, command, cancellationToken) is { } user
            ? Results.Ok(ApiResponse<UserDto>.Ok(user))
            : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay user."));
    }
}

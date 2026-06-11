using System.Security.Claims;
using LanguageLearning.API.Common;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Features.Courses;
using LanguageLearning.Domain;

namespace LanguageLearning.API.Features.Courses;

public static class CourseEndpoints
{
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/languages", async (
            ILearningCatalogService catalog,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<object>.Ok(
                await catalog.GetLanguagesAsync(cancellationToken))))
            .WithTags("Courses");

        var group = app.MapGroup("/api/courses").WithTags("Courses");
        group.MapGet("/", async (
            bool? includeDrafts,
            ClaimsPrincipal principal,
            ICourseManagementService service,
            CancellationToken cancellationToken) =>
        {
            var canViewDrafts = principal.Identity?.IsAuthenticated == true
                && principal.IsInRole(Roles.Admin);
            return Results.Ok(ApiResponse<IReadOnlyList<CourseSummaryDto>>.Ok(
                await service.GetAsync(includeDrafts == true && canViewDrafts, cancellationToken)));
        });
        group.MapGet("/{id:int}", async (
            int id,
            ICourseManagementService service,
            CancellationToken cancellationToken) =>
            await service.GetByIdAsync(id, cancellationToken) is { } course
                ? Results.Ok(ApiResponse<CourseSummaryDto>.Ok(course))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay khoa hoc.")));
        group.MapPost("/", CreateAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
        group.MapPut("/{id:int}", UpdateAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
        group.MapDelete("/{id:int}", async (
            int id,
            ICourseManagementService service,
            CancellationToken cancellationToken) =>
            await service.DeleteAsync(id, cancellationToken)
                ? Results.Ok(ApiResponse<object>.Ok(new { }, "Da xoa khoa hoc."))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay khoa hoc.")))
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
        group.MapPost("/{id:int}/publish", async (
            int id,
            ICourseManagementService service,
            CancellationToken cancellationToken) =>
            await service.PublishAsync(id, cancellationToken)
                ? Results.Ok(ApiResponse<object>.Ok(new { }, "Da publish khoa hoc."))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay khoa hoc.")))
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
        group.MapPost("/units", async (
            SaveUnitCommand command,
            ICourseManagementService service,
            CancellationToken cancellationToken) =>
        {
            if (command.CourseId <= 0 || string.IsNullOrWhiteSpace(command.Title))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Du lieu unit khong hop le."));
            }
            var id = await service.CreateUnitAsync(command, cancellationToken);
            return Results.Created($"/api/units/{id}", ApiResponse<object>.Ok(new { Id = id }));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Teacher));
        group.MapPost("/lessons", async (
            SaveLessonCommand command,
            ICourseManagementService service,
            CancellationToken cancellationToken) =>
        {
            if (command.UnitId <= 0 || string.IsNullOrWhiteSpace(command.Title))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Du lieu lesson khong hop le."));
            }
            var id = await service.CreateLessonAsync(command, cancellationToken);
            return Results.Created($"/api/lessons/{id}", ApiResponse<object>.Ok(new { Id = id }));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Teacher));

        return app;
    }

    private static async Task<IResult> CreateAsync(
        SaveCourseCommand command,
        ICourseManagementService service,
        CancellationToken cancellationToken)
    {
        var validation = Validate(command);
        if (validation is not null)
        {
            return Results.BadRequest(ApiResponse<object>.Fail(validation));
        }
        var course = await service.CreateAsync(command, cancellationToken);
        return Results.Created($"/api/courses/{course.Id}", ApiResponse<CourseSummaryDto>.Ok(course));
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        SaveCourseCommand command,
        ICourseManagementService service,
        CancellationToken cancellationToken)
    {
        var validation = Validate(command);
        if (validation is not null)
        {
            return Results.BadRequest(ApiResponse<object>.Fail(validation));
        }
        return await service.UpdateAsync(id, command, cancellationToken) is { } course
            ? Results.Ok(ApiResponse<CourseSummaryDto>.Ok(course))
            : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay khoa hoc."));
    }

    private static string? Validate(SaveCourseCommand command)
    {
        if (command.LanguageId <= 0 || string.IsNullOrWhiteSpace(command.Title))
        {
            return "LanguageId va Title la bat buoc.";
        }
        return string.IsNullOrWhiteSpace(command.Level) ? "Level la bat buoc." : null;
    }
}

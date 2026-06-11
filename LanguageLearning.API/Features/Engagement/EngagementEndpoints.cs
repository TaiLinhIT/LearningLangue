using System.Security.Claims;
using LanguageLearning.API.Common;
using LanguageLearning.Application.Features.Engagement;
using LanguageLearning.Domain;

namespace LanguageLearning.API.Features.Engagement;

public sealed record IpaSubmitRequest(int ExerciseId, string Answer);
public sealed record ReportRequest(string TargetType, int TargetId, string Reason);

public static class EngagementEndpoints
{
    public static IEndpointRouteBuilder MapEngagementEndpoints(this IEndpointRouteBuilder app)
    {
        var rankings = app.MapGroup("/api/rankings").WithTags("Rankings").RequireAuthorization();
        rankings.MapGet("/class/{classId:int}", async (
            int classId,
            IEngagementService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<RankingDto>>.Ok(
                await service.GetClassRankingAsync(classId, cancellationToken))));
        rankings.MapGet("/center", async (
            IEngagementService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<RankingDto>>.Ok(
                await service.GetCenterRankingAsync(cancellationToken))));
        rankings.MapGet("/student/me", async (
            ClaimsPrincipal principal,
            IEngagementService service,
            CancellationToken cancellationToken) =>
            await service.GetStudentRankingAsync(principal.GetUserId(), cancellationToken) is { } ranking
                ? Results.Ok(ApiResponse<RankingDto>.Ok(ranking))
                : Results.NotFound(ApiResponse<object>.Fail("Chua co du lieu xep hang.")));

        var rewards = app.MapGroup("/api/rewards").WithTags("Rewards").RequireAuthorization();
        rewards.MapGet("/", async (
            ClaimsPrincipal principal,
            IEngagementService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<RewardDto>>.Ok(
                await service.GetRewardsAsync(principal.GetUserId(), cancellationToken))));
        rewards.MapPost("/", async (
            SaveRewardCommand command,
            IEngagementService service,
            CancellationToken cancellationToken) =>
        {
            var reward = await service.CreateRewardAsync(command, cancellationToken);
            return Results.Created($"/api/rewards/{reward.Id}", ApiResponse<RewardDto>.Ok(reward));
        }).RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
        rewards.MapPost("/calculate", async (
            IEngagementService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<object>.Ok(
                new { Awarded = await service.CalculateRewardsAsync(cancellationToken) },
                "Da tinh thuong.")))
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
        app.MapGet("/api/students/me/rewards", async (
            ClaimsPrincipal principal,
            IEngagementService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<RewardDto>>.Ok(
                await service.GetRewardsAsync(principal.GetUserId(), cancellationToken))))
            .RequireAuthorization()
            .WithTags("Rewards");

        var notifications = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();
        notifications.MapGet("/", async (
            ClaimsPrincipal principal,
            IEngagementService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(
                await service.GetNotificationsAsync(principal.GetUserId(), cancellationToken))));
        notifications.MapPost("/read/{id:int}", async (
            int id,
            ClaimsPrincipal principal,
            IEngagementService service,
            CancellationToken cancellationToken) =>
            await service.MarkNotificationReadAsync(principal.GetUserId(), id, cancellationToken)
                ? Results.Ok(ApiResponse<object>.Ok(new { }, "Da danh dau da doc."))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay notification.")));

        var ipa = app.MapGroup("/api/ipa").WithTags("IPA");
        ipa.MapGet("/sounds", async (
            IEngagementService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<IpaSoundDto>>.Ok(
                await service.GetIpaSoundsAsync(cancellationToken))));
        ipa.MapGet("/lessons", async (
            IEngagementService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<IpaLessonDto>>.Ok(
                await service.GetIpaLessonsAsync(cancellationToken))));
        ipa.MapGet("/exercises", async (
            IEngagementService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<IpaExerciseDto>>.Ok(
                await service.GetIpaExercisesAsync(cancellationToken))));
        ipa.MapPost("/exercises/submit", async (
            IpaSubmitRequest request,
            IEngagementService service,
            CancellationToken cancellationToken) =>
            await service.SubmitIpaExerciseAsync(request.ExerciseId, request.Answer, cancellationToken) is { } result
                ? Results.Ok(ApiResponse<IpaSubmissionResult>.Ok(result))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay bai tap IPA.")))
            .RequireAuthorization();

        var reports = app.MapGroup("/api/reports").WithTags("Reports").RequireAuthorization();
        reports.MapPost("/", async (
            ReportRequest request,
            ClaimsPrincipal principal,
            IEngagementService service,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.TargetType) || string.IsNullOrWhiteSpace(request.Reason))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("TargetType va Reason la bat buoc."));
            }
            var report = await service.CreateReportAsync(
                principal.GetUserId(),
                request.TargetType,
                request.TargetId,
                request.Reason,
                cancellationToken);
            return Results.Created($"/api/reports/{report.Id}", ApiResponse<ReportDto>.Ok(report));
        });
        reports.MapGet("/", async (
            IEngagementService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<ReportDto>>.Ok(
                await service.GetReportsAsync(cancellationToken))))
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Teacher));

        return app;
    }
}

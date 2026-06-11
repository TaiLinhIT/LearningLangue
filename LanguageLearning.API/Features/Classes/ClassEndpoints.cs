using System.Security.Claims;
using LanguageLearning.API.Common;
using LanguageLearning.Application.Features.Classes;
using LanguageLearning.Domain;
using Microsoft.AspNetCore.SignalR;

namespace LanguageLearning.API.Features.Classes;

public sealed record CommentRequest(string Content);

public static class ClassEndpoints
{
    public static IEndpointRouteBuilder MapClassEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/classes").WithTags("Classes").RequireAuthorization();
        group.MapGet("/", GetClassesAsync);
        group.MapGet("/{id:int}", async (
            int id,
            IClassroomService service,
            CancellationToken cancellationToken) =>
            await service.GetClassAsync(id, cancellationToken) is { } item
                ? Results.Ok(ApiResponse<ClassDto>.Ok(item))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay lop hoc.")));
        group.MapPost("/", CreateAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Receptionist));
        group.MapPut("/{id:int}", UpdateAsync)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Receptionist));
        group.MapPost("/{id:int}/assign-student", async (
            int id,
            AssignStudentCommand command,
            IClassroomService service,
            CancellationToken cancellationToken) =>
            await service.AssignStudentAsync(id, command, cancellationToken)
                ? Results.Ok(ApiResponse<object>.Ok(new { }, "Da gan hoc vien vao lop."))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay lop hoac hoc vien.")))
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Receptionist));
        group.MapGet("/{id:int}/students", async (
            int id,
            IClassroomService service,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<IReadOnlyList<ClassStudentDto>>.Ok(
                await service.GetStudentsAsync(id, cancellationToken))));

        group.MapGet("/{classId:int}/comments", async (
            int classId,
            ClaimsPrincipal principal,
            IClassroomService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(ApiResponse<IReadOnlyList<CommentDto>>.Ok(
                    await service.GetCommentsAsync(
                        classId,
                        principal.GetUserId(),
                        principal.GetRole(),
                        cancellationToken)));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });
        group.MapPost("/{classId:int}/comments", AddCommentAsync);

        app.MapPost("/api/comments/{commentId:int}/replies", AddReplyAsync)
            .RequireAuthorization()
            .WithTags("Classes");
        app.MapPost("/api/comments/{commentId:int}/pin", async (
            int commentId,
            IClassroomService service,
            CancellationToken cancellationToken) =>
            await service.PinCommentAsync(commentId, cancellationToken)
                ? Results.Ok(ApiResponse<object>.Ok(new { }, "Da cap nhat trang thai ghim."))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay comment.")))
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Teacher))
            .WithTags("Classes");
        app.MapDelete("/api/comments/{commentId:int}", async (
            int commentId,
            ClaimsPrincipal principal,
            IClassroomService service,
            CancellationToken cancellationToken) =>
            await service.DeleteCommentAsync(
                commentId,
                principal.GetUserId(),
                principal.GetRole(),
                cancellationToken)
                ? Results.Ok(ApiResponse<object>.Ok(new { }, "Da xoa comment."))
                : Results.Forbid())
            .RequireAuthorization()
            .WithTags("Classes");

        app.MapHub<ClassDiscussionHub>("/hubs/class-discussion");
        return app;
    }

    private static async Task<IResult> GetClassesAsync(
        ClaimsPrincipal principal,
        IClassroomService service,
        CancellationToken cancellationToken) =>
        Results.Ok(ApiResponse<IReadOnlyList<ClassDto>>.Ok(
            await service.GetClassesAsync(
                principal.GetUserId(),
                principal.GetRole(),
                cancellationToken)));

    private static async Task<IResult> CreateAsync(
        SaveClassCommand command,
        IClassroomService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await service.CreateAsync(command, cancellationToken);
            return Results.Created($"/api/classes/{item.Id}", ApiResponse<ClassDto>.Ok(item));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        SaveClassCommand command,
        IClassroomService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return await service.UpdateAsync(id, command, cancellationToken) is { } item
                ? Results.Ok(ApiResponse<ClassDto>.Ok(item))
                : Results.NotFound(ApiResponse<object>.Fail("Khong tim thay lop hoc."));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    private static async Task<IResult> AddCommentAsync(
        int classId,
        CommentRequest request,
        ClaimsPrincipal principal,
        IClassroomService service,
        IHubContext<ClassDiscussionHub> hub,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest(ApiResponse<object>.Fail("Noi dung comment la bat buoc."));
        }
        try
        {
            var comment = await service.AddCommentAsync(
                classId,
                principal.GetUserId(),
                principal.GetRole(),
                request.Content,
                cancellationToken);
            await hub.Clients.Group(ClassDiscussionHub.GroupName(classId))
                .SendAsync("CommentCreated", comment, cancellationToken);
            return Results.Created(
                $"/api/classes/{classId}/comments/{comment!.Id}",
                ApiResponse<CommentDto>.Ok(comment));
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> AddReplyAsync(
        int commentId,
        CommentRequest request,
        ClaimsPrincipal principal,
        IClassroomService service,
        IHubContext<ClassDiscussionHub> hub,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest(ApiResponse<object>.Fail("Noi dung reply la bat buoc."));
        }
        try
        {
            var reply = await service.AddReplyAsync(
                commentId,
                principal.GetUserId(),
                request.Content,
                cancellationToken);
            if (reply is null)
            {
                return Results.NotFound(ApiResponse<object>.Fail("Khong tim thay comment."));
            }
            await hub.Clients.All.SendAsync("CommentReplied", commentId, reply, cancellationToken);
            return Results.Created(
                $"/api/comments/{commentId}/replies/{reply.Id}",
                ApiResponse<CommentReplyDto>.Ok(reply));
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }
}

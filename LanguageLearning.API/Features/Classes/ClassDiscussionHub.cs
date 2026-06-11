using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LanguageLearning.API.Features.Classes;

[Authorize]
public sealed class ClassDiscussionHub : Hub
{
    public Task JoinClass(int classId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(classId));

    public Task LeaveClass(int classId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(classId));

    public static string GroupName(int classId) => $"class-{classId}";
}

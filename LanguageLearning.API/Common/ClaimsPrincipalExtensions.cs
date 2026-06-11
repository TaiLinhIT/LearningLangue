using System.Security.Claims;

namespace LanguageLearning.API.Common;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Missing user id claim."));

    public static string GetRole(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public static string GetSessionToken(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("session_token")
            ?? throw new UnauthorizedAccessException("Missing session token claim.");
}

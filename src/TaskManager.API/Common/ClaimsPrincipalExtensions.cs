using System.Security.Claims;

namespace TaskManager.API.Common;

/// <summary>Helpers for reading identity information off the authenticated principal.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the current user's id from the JWT. Throws if absent, which should never
    /// happen on an [Authorize] endpoint and indicates a misconfigured token pipeline.
    /// </summary>
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated request is missing a user id claim.");
    }
}

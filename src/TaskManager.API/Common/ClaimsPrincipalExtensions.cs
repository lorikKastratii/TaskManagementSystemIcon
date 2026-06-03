using System.Security.Claims;
using TaskManager.Application.Common;

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

    /// <summary>True when the caller holds the Admin role.</summary>
    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole("Admin");

    /// <summary>Returns the role names carried by the JWT.</summary>
    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal principal) =>
        principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

    /// <summary>Builds the Application-layer caller context from the validated principal.</summary>
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal) =>
        new(principal.GetUserId(), principal.IsAdmin());
}

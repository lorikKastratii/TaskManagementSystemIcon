namespace TaskManager.Application.Users.Dtos;

/// <summary>
/// Lightweight read model describing an account that a task can be assigned to.
/// Returned by the admin-only users endpoint and used to resolve assignee names for tasks.
/// </summary>
public record UserSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    /// <summary>Friendly name for display; falls back to the email when no display name is set.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>True when this account holds the Admin role.</summary>
    public bool IsAdmin { get; init; }
}

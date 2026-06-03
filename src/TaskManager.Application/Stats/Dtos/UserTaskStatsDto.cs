namespace TaskManager.Application.Stats.Dtos;

/// <summary>Per-user task tallies for the dashboard "completed by user" leaderboard.</summary>
public record UserTaskStatsDto
{
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Total tasks currently assigned to this user.</summary>
    public int Assigned { get; init; }

    /// <summary>How many of the assigned tasks are completed.</summary>
    public int Completed { get; init; }

    /// <summary>Assigned tasks that are not yet completed.</summary>
    public int Active { get; init; }
}

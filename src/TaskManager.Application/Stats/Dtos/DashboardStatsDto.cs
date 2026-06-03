namespace TaskManager.Application.Stats.Dtos;

/// <summary>
/// Aggregate metrics for the dashboard. The board is shared, so these numbers span every task
/// in the system. Breakdowns are keyed by the string enum name (Todo/InProgress/... and
/// Low/Medium/High) to match how the rest of the API serialises enums.
/// </summary>
public record DashboardStatsDto
{
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int ActiveTasks { get; init; }

    /// <summary>Active tasks whose due date has passed.</summary>
    public int OverdueTasks { get; init; }

    public int UnassignedTasks { get; init; }

    /// <summary>Share of tasks that are completed, 0..1 (0 when there are no tasks).</summary>
    public double CompletionRate { get; init; }

    public IReadOnlyDictionary<string, int> TasksByStatus { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> TasksByPriority { get; init; } = new Dictionary<string, int>();

    /// <summary>Per-user tallies, ordered by completed then assigned (top contributors first).</summary>
    public IReadOnlyList<UserTaskStatsDto> PerUser { get; init; } = Array.Empty<UserTaskStatsDto>();
}

using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Dtos;

/// <summary>
/// Optional filters for listing tasks. Bound from the query string on GET /api/tasks.
/// Any combination may be supplied; null members are ignored.
/// </summary>
public record TaskQueryParameters
{
    /// <summary>Filter by workflow status (Todo/InProgress/Done).</summary>
    public TaskItemStatus? Status { get; init; }

    /// <summary>Filter by priority (Low/Medium/High).</summary>
    public TaskPriority? Priority { get; init; }

    /// <summary>Filter by completion flag.</summary>
    public bool? IsCompleted { get; init; }

    /// <summary>Case-insensitive substring match against title and description.</summary>
    public string? Search { get; init; }
}

using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Dtos;

/// <summary>
/// Read model returned to clients. Exposes enums as strings (via JSON enum converter)
/// and includes audit timestamps so the UI can display creation/update times.
/// </summary>
public record TaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskItemStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public DateTime? DueDate { get; init; }
    public bool IsCompleted { get; init; }
    public int SortOrder { get; init; }

    /// <summary>Id of the user this task is assigned to, or null if unassigned.</summary>
    public string? AssigneeId { get; init; }

    /// <summary>Display name (or email) of the assignee, resolved for the UI. Null when unassigned.</summary>
    public string? AssigneeName { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

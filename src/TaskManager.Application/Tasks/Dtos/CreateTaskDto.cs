using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Dtos;

/// <summary>Payload for creating a new task. Validated by CreateTaskDtoValidator.</summary>
public record CreateTaskDto
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskItemStatus Status { get; init; } = TaskItemStatus.Todo;
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;
    public DateTime? DueDate { get; init; }

    /// <summary>
    /// Optional id of the user to assign the new task to. Honoured only for admins; a regular
    /// user's task is always assigned to themselves regardless of this value.
    /// </summary>
    public string? AssigneeId { get; init; }
}

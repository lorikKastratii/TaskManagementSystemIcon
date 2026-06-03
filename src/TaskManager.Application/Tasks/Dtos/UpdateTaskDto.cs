using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Dtos;

/// <summary>
/// Payload for updating a task. All fields are optional so callers can perform partial
/// updates; only non-null values are applied to the entity. Validated by UpdateTaskDtoValidator.
/// </summary>
public record UpdateTaskDto
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public TaskItemStatus? Status { get; init; }
    public TaskPriority? Priority { get; init; }
    public DateTime? DueDate { get; init; }
    public bool? IsCompleted { get; init; }
}

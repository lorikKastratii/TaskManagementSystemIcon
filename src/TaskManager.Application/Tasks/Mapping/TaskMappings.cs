using TaskManager.Application.Tasks.Dtos;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks.Mapping;

/// <summary>
/// Explicit, allocation-light mapping between <see cref="TaskItem"/> entities and DTOs.
/// Preferred over a convention-based mapper here: the mappings are trivial, fully visible,
/// trivially unit-testable, and carry no third-party licensing constraints.
/// </summary>
public static class TaskMappings
{
    /// <summary>
    /// Projects an entity to its read DTO. The assignee's display name is resolved by the caller
    /// (it lives in Identity, outside the entity) and passed in; null leaves AssigneeName empty.
    /// </summary>
    public static TaskDto ToDto(this TaskItem task, string? assigneeName = null) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status,
        Priority = task.Priority,
        DueDate = task.DueDate,
        IsCompleted = task.IsCompleted,
        SortOrder = task.SortOrder,
        AssigneeId = task.AssigneeId,
        AssigneeName = assigneeName,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };

    /// <summary>Builds a new entity from a create request. Caller assigns Id/UserId/SortOrder/CreatedAt.</summary>
    public static TaskItem ToEntity(this CreateTaskDto dto) => new()
    {
        Title = dto.Title.Trim(),
        Description = dto.Description?.Trim(),
        Status = dto.Status,
        Priority = dto.Priority,
        DueDate = dto.DueDate,
        IsCompleted = dto.Status == Domain.Enums.TaskItemStatus.Done
    };

    /// <summary>
    /// Applies the non-null fields of an update request onto an existing entity (partial update).
    /// Keeps the Status/IsCompleted invariant consistent via the domain method.
    /// </summary>
    public static void ApplyUpdate(this TaskItem task, UpdateTaskDto dto)
    {
        if (dto.Title is not null) task.Title = dto.Title.Trim();
        if (dto.Description is not null) task.Description = dto.Description.Trim();
        if (dto.Priority.HasValue) task.Priority = dto.Priority.Value;
        if (dto.DueDate.HasValue) task.DueDate = dto.DueDate;
        if (dto.Status.HasValue) task.Status = dto.Status.Value;

        // Completion flag wins last so Status/IsCompleted stay consistent.
        if (dto.IsCompleted.HasValue)
        {
            task.SetCompletion(dto.IsCompleted.Value);
        }
        else if (dto.Status.HasValue)
        {
            // Keep IsCompleted aligned when only the status changed.
            task.IsCompleted = dto.Status.Value == Domain.Enums.TaskItemStatus.Done;
        }
    }
}

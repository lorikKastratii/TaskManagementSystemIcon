using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Entities;

/// <summary>
/// The core aggregate of the system: a single task owned by a user.
/// Encapsulates its own state transitions so business rules (e.g. "completing a task
/// moves it to Done") live in the domain rather than leaking into services.
/// </summary>
public class TaskItem : BaseEntity
{
    /// <summary>Short, required summary of the task (max 200 chars, enforced by validation/EF).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description (max 1000 chars).</summary>
    public string? Description { get; set; }

    /// <summary>Current workflow state. Drives status-based filtering.</summary>
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    /// <summary>Relative importance, used for prioritisation (bonus feature).</summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>Optional deadline (UTC).</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Convenience completion flag for the "mark complete/incomplete" feature.
    /// Kept in sync with <see cref="Status"/> via the domain methods below.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>Manual ordering position within the owner's list (supports drag-and-drop sorting).</summary>
    public int SortOrder { get; set; }

    /// <summary>Identifier of the owning user (ASP.NET Identity user id). Enforces per-user isolation.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Marks the task as complete or incomplete, keeping <see cref="Status"/> consistent.
    /// Completing always moves the task to Done; re-opening a Done task returns it to Todo.
    /// </summary>
    public void SetCompletion(bool completed)
    {
        IsCompleted = completed;
        if (completed)
        {
            Status = TaskItemStatus.Done;
        }
        else if (Status == TaskItemStatus.Done)
        {
            Status = TaskItemStatus.Todo;
        }
    }
}

using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks.Interfaces;

/// <summary>
/// Persistence contract for tasks. Implemented in the Infrastructure layer.
/// Reads come in two shapes: a single assignee's list (regular users) and the whole table
/// (admins). Access control for individual tasks is enforced by <c>TaskService</c>, not here.
/// </summary>
public interface ITaskRepository
{
    /// <summary>Returns all tasks in the system, ordered by SortOrder then CreatedAt. Admin view.</summary>
    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns the tasks assigned to a user, ordered by SortOrder then CreatedAt.</summary>
    Task<IReadOnlyList<TaskItem>> GetForAssigneeAsync(string assigneeId, CancellationToken ct = default);

    /// <summary>Returns a single task by id, or null if it does not exist. Not access-scoped.</summary>
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the highest SortOrder used within an assignee's list (0 if none), for appending.</summary>
    Task<int> GetMaxSortOrderAsync(string assigneeId, CancellationToken ct = default);

    /// <summary>Adds a new task and persists it.</summary>
    Task AddAsync(TaskItem task, CancellationToken ct = default);

    /// <summary>Persists pending changes to a tracked task.</summary>
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);

    /// <summary>Persists SortOrder changes for a batch of tasks (drag-and-drop reorder).</summary>
    Task UpdateRangeAsync(IEnumerable<TaskItem> tasks, CancellationToken ct = default);

    /// <summary>Removes a task.</summary>
    Task DeleteAsync(TaskItem task, CancellationToken ct = default);
}

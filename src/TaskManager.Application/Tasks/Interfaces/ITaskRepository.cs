using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks.Interfaces;

/// <summary>
/// Persistence contract for tasks. Implemented in the Infrastructure layer.
/// All queries are scoped by <paramref name="userId"/> to enforce per-user data isolation.
/// </summary>
public interface ITaskRepository
{
    /// <summary>Returns all tasks owned by the user, ordered by SortOrder then CreatedAt.</summary>
    Task<IReadOnlyList<TaskItem>> GetAllAsync(string userId, CancellationToken ct = default);

    /// <summary>Returns a single task owned by the user, or null if it does not exist / is not theirs.</summary>
    Task<TaskItem?> GetByIdAsync(string userId, Guid id, CancellationToken ct = default);

    /// <summary>Returns the highest SortOrder currently used by the user (0 if none), for appending new tasks.</summary>
    Task<int> GetMaxSortOrderAsync(string userId, CancellationToken ct = default);

    /// <summary>Adds a new task and persists it.</summary>
    Task AddAsync(TaskItem task, CancellationToken ct = default);

    /// <summary>Persists pending changes to a tracked task.</summary>
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);

    /// <summary>Persists SortOrder changes for a batch of tasks (drag-and-drop reorder).</summary>
    Task UpdateRangeAsync(IEnumerable<TaskItem> tasks, CancellationToken ct = default);

    /// <summary>Removes a task.</summary>
    Task DeleteAsync(TaskItem task, CancellationToken ct = default);
}

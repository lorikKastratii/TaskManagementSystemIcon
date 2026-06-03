using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks.Interfaces;

/// <summary>
/// Persistence contract for tasks, implemented in the Infrastructure layer. Reads are async;
/// writes only register the change against the context — committing is the caller's
/// responsibility via <see cref="Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TaskItem>> GetForAssigneeAsync(string assigneeId, CancellationToken ct = default);

    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Highest SortOrder within an assignee's list (0 if none), used when appending.</summary>
    Task<int> GetMaxSortOrderAsync(string assigneeId, CancellationToken ct = default);

    void Add(TaskItem task);

    void Update(TaskItem task);

    void UpdateRange(IEnumerable<TaskItem> tasks);

    void Remove(TaskItem task);
}

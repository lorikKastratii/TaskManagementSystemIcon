using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Data;

namespace TaskManager.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITaskRepository"/>. Writes register changes on the
/// context; the <see cref="UnitOfWork"/> commits them.
/// </summary>
public class TaskRepository : ITaskRepository
{
    private readonly TaskDbContext _db;

    public TaskRepository(TaskDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default)
    {
        return await OrderedTasks(_db.Tasks).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TaskItem>> GetForAssigneeAsync(string assigneeId, CancellationToken ct = default)
    {
        var query = _db.Tasks.Where(t => t.AssigneeId == assigneeId);
        return await OrderedTasks(query).ToListAsync(ct);
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<int> GetMaxSortOrderAsync(string assigneeId, CancellationToken ct = default)
    {
        var assigneeTasks = _db.Tasks.Where(t => t.AssigneeId == assigneeId);
        if (!await assigneeTasks.AnyAsync(ct))
        {
            return 0;
        }

        return await assigneeTasks.MaxAsync(t => t.SortOrder, ct);
    }

    public void Add(TaskItem task) => _db.Tasks.Add(task);

    public void Update(TaskItem task) => _db.Tasks.Update(task);

    public void UpdateRange(IEnumerable<TaskItem> tasks) => _db.Tasks.UpdateRange(tasks);

    public void Remove(TaskItem task) => _db.Tasks.Remove(task);

    private static IQueryable<TaskItem> OrderedTasks(IQueryable<TaskItem> query)
    {
        return query
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt);
    }
}

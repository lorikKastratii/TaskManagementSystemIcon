using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Data;

namespace TaskManager.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITaskRepository"/>. Provides per-assignee reads for
/// regular users and a full-table read for admins. Access decisions for individual tasks are
/// made by the service layer.
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
        return await _db.Tasks
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TaskItem>> GetForAssigneeAsync(string assigneeId, CancellationToken ct = default)
    {
        return await _db.Tasks
            .Where(t => t.AssigneeId == assigneeId)
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<int> GetMaxSortOrderAsync(string assigneeId, CancellationToken ct = default)
    {
        var hasTasks = await _db.Tasks.AnyAsync(t => t.AssigneeId == assigneeId, ct);
        if (!hasTasks)
        {
            return 0;
        }

        return await _db.Tasks
            .Where(t => t.AssigneeId == assigneeId)
            .MaxAsync(t => t.SortOrder, ct);
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        _db.Tasks.Update(task);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(IEnumerable<TaskItem> tasks, CancellationToken ct = default)
    {
        _db.Tasks.UpdateRange(tasks);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(TaskItem task, CancellationToken ct = default)
    {
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(ct);
    }
}

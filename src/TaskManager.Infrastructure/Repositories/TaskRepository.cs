using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Data;

namespace TaskManager.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITaskRepository"/>. Every read is filtered by UserId
/// at the database level so a user can never load another user's tasks.
/// </summary>
public class TaskRepository : ITaskRepository
{
    private readonly TaskDbContext _db;

    public TaskRepository(TaskDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(string userId, CancellationToken ct = default)
    {
        return await _db.Tasks
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<TaskItem?> GetByIdAsync(string userId, Guid id, CancellationToken ct = default)
    {
        return _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
    }

    public async Task<int> GetMaxSortOrderAsync(string userId, CancellationToken ct = default)
    {
        var hasTasks = await _db.Tasks.AnyAsync(t => t.UserId == userId, ct);
        if (!hasTasks)
        {
            return 0;
        }

        return await _db.Tasks
            .Where(t => t.UserId == userId)
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

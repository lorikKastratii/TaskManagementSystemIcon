using TaskManager.Application.Common.Interfaces;

namespace TaskManager.Infrastructure.Data;

/// <summary>EF Core unit of work over <see cref="TaskDbContext"/>.</summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly TaskDbContext _db;

    public UnitOfWork(TaskDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }
}

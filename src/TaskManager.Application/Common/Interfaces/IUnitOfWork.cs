namespace TaskManager.Application.Common.Interfaces;

/// <summary>
/// Commit boundary for a single business operation. Repositories register changes against the
/// shared context; calling <see cref="SaveChangesAsync"/> persists them atomically.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists all pending changes and returns the number of affected rows.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

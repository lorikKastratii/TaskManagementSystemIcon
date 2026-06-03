namespace TaskManager.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Provides the primary key and audit timestamps
/// so that every aggregate shares a consistent identity and tracking contract.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Globally unique identifier for the entity.</summary>
    public Guid Id { get; set; }

    /// <summary>UTC timestamp set once when the entity is first persisted.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp updated on every subsequent modification; null until first edit.</summary>
    public DateTime? UpdatedAt { get; set; }
}

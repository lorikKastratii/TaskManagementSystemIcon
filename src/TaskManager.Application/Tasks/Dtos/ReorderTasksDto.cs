namespace TaskManager.Application.Tasks.Dtos;

/// <summary>
/// Payload for persisting a drag-and-drop reordering. The client sends the task ids in
/// their new visual order; the server assigns SortOrder = index for each.
/// </summary>
public record ReorderTasksDto
{
    /// <summary>Task ids in their new top-to-bottom order.</summary>
    public IReadOnlyList<Guid> OrderedTaskIds { get; init; } = Array.Empty<Guid>();
}

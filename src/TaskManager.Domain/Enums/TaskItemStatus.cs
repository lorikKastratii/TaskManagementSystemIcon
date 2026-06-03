namespace TaskManager.Domain.Enums;

/// <summary>
/// Workflow state of a task. Persisted as a string in the database (see TaskDbContext)
/// so the values remain human-readable and stable even if the numeric order changes.
/// </summary>
public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}

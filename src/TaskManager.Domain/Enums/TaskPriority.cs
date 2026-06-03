namespace TaskManager.Domain.Enums;

/// <summary>
/// Relative importance of a task, used for sorting and visual emphasis in the UI.
/// Persisted as a string in the database for readability.
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}

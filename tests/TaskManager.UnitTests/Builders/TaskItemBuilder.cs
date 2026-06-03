using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.UnitTests.Builders;

/// <summary>
/// Fluent builder for <see cref="TaskItem"/> test data. Keeps individual tests readable by
/// expressing only the fields that matter to the scenario and defaulting the rest.
/// </summary>
public class TaskItemBuilder
{
    private readonly TaskItem _task = new()
    {
        Id = Guid.NewGuid(),
        Title = "Sample task",
        Description = "Sample description",
        Status = TaskItemStatus.Todo,
        Priority = TaskPriority.Medium,
        IsCompleted = false,
        SortOrder = 0,
        UserId = "user-1",
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    public static TaskItemBuilder ForUser(string userId) => new TaskItemBuilder().WithUser(userId);

    public TaskItemBuilder WithId(Guid id) { _task.Id = id; return this; }
    public TaskItemBuilder WithUser(string userId) { _task.UserId = userId; return this; }
    public TaskItemBuilder WithTitle(string title) { _task.Title = title; return this; }
    public TaskItemBuilder WithStatus(TaskItemStatus status) { _task.Status = status; return this; }
    public TaskItemBuilder WithPriority(TaskPriority priority) { _task.Priority = priority; return this; }
    public TaskItemBuilder WithSortOrder(int order) { _task.SortOrder = order; return this; }
    public TaskItemBuilder Completed(bool completed = true) { _task.SetCompletion(completed); return this; }

    public TaskItem Build() => _task;
}

using TaskManager.Application.Common;
using TaskManager.Application.Tasks.Dtos;

namespace TaskManager.Application.Tasks.Interfaces;

/// <summary>
/// Application service exposing task use-cases to the API layer. Every operation takes the
/// authenticated <see cref="CurrentUser"/>: a regular user may only see and mutate tasks
/// assigned to them, while an admin may see all tasks and (re)assign them to any user.
/// Operations throw NotFoundException when a task is missing or out of the caller's reach,
/// so callers cannot probe other users' data.
/// </summary>
public interface ITaskService
{
    Task<IReadOnlyList<TaskDto>> GetTasksAsync(CurrentUser caller, TaskQueryParameters query, CancellationToken ct = default);
    Task<TaskDto> GetByIdAsync(CurrentUser caller, Guid id, CancellationToken ct = default);
    Task<TaskDto> CreateAsync(CurrentUser caller, CreateTaskDto dto, CancellationToken ct = default);
    Task<TaskDto> UpdateAsync(CurrentUser caller, Guid id, UpdateTaskDto dto, CancellationToken ct = default);
    Task<TaskDto> SetCompletionAsync(CurrentUser caller, Guid id, bool isCompleted, CancellationToken ct = default);

    /// <summary>Admin-only: (re)assigns a task to a user (or unassigns when assigneeId is null).</summary>
    Task<TaskDto> AssignAsync(CurrentUser caller, Guid id, string? assigneeId, CancellationToken ct = default);

    Task ReorderAsync(CurrentUser caller, ReorderTasksDto dto, CancellationToken ct = default);
    Task DeleteAsync(CurrentUser caller, Guid id, CancellationToken ct = default);
}

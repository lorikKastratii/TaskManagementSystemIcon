using TaskManager.Application.Tasks.Dtos;

namespace TaskManager.Application.Tasks.Interfaces;

/// <summary>
/// Application service exposing task use-cases to the API layer. Every operation is scoped
/// to a user id (extracted from the JWT) and throws NotFoundException when a task is missing
/// or owned by someone else, ensuring callers cannot probe other users' data.
/// </summary>
public interface ITaskService
{
    Task<IReadOnlyList<TaskDto>> GetTasksAsync(string userId, TaskQueryParameters query, CancellationToken ct = default);
    Task<TaskDto> GetByIdAsync(string userId, Guid id, CancellationToken ct = default);
    Task<TaskDto> CreateAsync(string userId, CreateTaskDto dto, CancellationToken ct = default);
    Task<TaskDto> UpdateAsync(string userId, Guid id, UpdateTaskDto dto, CancellationToken ct = default);
    Task<TaskDto> SetCompletionAsync(string userId, Guid id, bool isCompleted, CancellationToken ct = default);
    Task ReorderAsync(string userId, ReorderTasksDto dto, CancellationToken ct = default);
    Task DeleteAsync(string userId, Guid id, CancellationToken ct = default);
}

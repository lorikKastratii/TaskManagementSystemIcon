using Microsoft.Extensions.Logging;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Application.Tasks.Mapping;

namespace TaskManager.Application.Tasks.Services;

/// <summary>
/// Coordinates task use-cases: ownership enforcement, default values, the Status/IsCompleted
/// invariant, filtering and drag-and-drop reordering. Pure orchestration — persistence is
/// delegated to <see cref="ITaskRepository"/>, which makes this class fully unit-testable.
/// </summary>
public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly ILogger<TaskService> _logger;

    public TaskService(ITaskRepository repository, ILogger<TaskService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(string userId, TaskQueryParameters query, CancellationToken ct = default)
    {
        var tasks = await _repository.GetAllAsync(userId, ct);

        // Filtering is applied in-memory: a single user's task list is small, and this keeps
        // the repository contract simple and the filter logic trivially testable.
        IEnumerable<Domain.Entities.TaskItem> filtered = tasks;

        if (query.Status.HasValue)
            filtered = filtered.Where(t => t.Status == query.Status.Value);

        if (query.Priority.HasValue)
            filtered = filtered.Where(t => t.Priority == query.Priority.Value);

        if (query.IsCompleted.HasValue)
            filtered = filtered.Where(t => t.IsCompleted == query.IsCompleted.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            filtered = filtered.Where(t =>
                t.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (t.Description is not null && t.Description.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        return filtered.Select(t => t.ToDto()).ToList();
    }

    public async Task<TaskDto> GetByIdAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(userId, id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.TaskItem), id);

        return task.ToDto();
    }

    public async Task<TaskDto> CreateAsync(string userId, CreateTaskDto dto, CancellationToken ct = default)
    {
        var task = dto.ToEntity();
        task.Id = Guid.NewGuid();
        task.UserId = userId;
        task.CreatedAt = DateTime.UtcNow;
        // New tasks append to the end of the user's list.
        task.SortOrder = await _repository.GetMaxSortOrderAsync(userId, ct) + 1;

        await _repository.AddAsync(task, ct);
        _logger.LogInformation("Created task {TaskId} for user {UserId}", task.Id, userId);

        return task.ToDto();
    }

    public async Task<TaskDto> UpdateAsync(string userId, Guid id, UpdateTaskDto dto, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(userId, id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.TaskItem), id);

        task.ApplyUpdate(dto);
        task.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(task, ct);
        _logger.LogInformation("Updated task {TaskId} for user {UserId}", id, userId);

        return task.ToDto();
    }

    public async Task<TaskDto> SetCompletionAsync(string userId, Guid id, bool isCompleted, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(userId, id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.TaskItem), id);

        task.SetCompletion(isCompleted);
        task.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(task, ct);
        return task.ToDto();
    }

    public async Task ReorderAsync(string userId, ReorderTasksDto dto, CancellationToken ct = default)
    {
        var tasks = await _repository.GetAllAsync(userId, ct);
        var byId = tasks.ToDictionary(t => t.Id);

        // Assign SortOrder by the position of each id in the supplied order. Ids the user does
        // not own (or that no longer exist) are ignored so a stale client cannot corrupt data.
        var changed = new List<Domain.Entities.TaskItem>();
        for (var index = 0; index < dto.OrderedTaskIds.Count; index++)
        {
            if (byId.TryGetValue(dto.OrderedTaskIds[index], out var task) && task.SortOrder != index)
            {
                task.SortOrder = index;
                task.UpdatedAt = DateTime.UtcNow;
                changed.Add(task);
            }
        }

        if (changed.Count > 0)
        {
            await _repository.UpdateRangeAsync(changed, ct);
            _logger.LogInformation("Reordered {Count} tasks for user {UserId}", changed.Count, userId);
        }
    }

    public async Task DeleteAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(userId, id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.TaskItem), id);

        await _repository.DeleteAsync(task, ct);
        _logger.LogInformation("Deleted task {TaskId} for user {UserId}", id, userId);
    }
}

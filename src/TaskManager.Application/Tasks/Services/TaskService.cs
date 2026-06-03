using Microsoft.Extensions.Logging;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Application.Tasks.Mapping;
using TaskManager.Application.Users.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks.Services;

/// <summary>
/// Coordinates task use-cases: assignment-based access control, default values, the
/// Status/IsCompleted invariant, filtering and drag-and-drop reordering. Pure orchestration —
/// persistence is delegated to <see cref="ITaskRepository"/> and account lookups to
/// <see cref="IUserDirectory"/>, which keeps this class fully unit-testable.
///
/// Access model: a regular user sees and mutates only tasks assigned to them; an admin sees
/// all tasks and may (re)assign them to anyone.
/// </summary>
public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly IUserDirectory _users;
    private readonly ILogger<TaskService> _logger;

    public TaskService(ITaskRepository repository, IUserDirectory users, ILogger<TaskService> logger)
    {
        _repository = repository;
        _users = users;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(CurrentUser caller, TaskQueryParameters query, CancellationToken ct = default)
    {
        // Admins see the whole board (optionally narrowed to one assignee); everyone else sees
        // only the tasks assigned to them.
        var tasks = caller.IsAdmin
            ? await _repository.GetAllAsync(ct)
            : await _repository.GetForAssigneeAsync(caller.Id, ct);

        // Filtering is applied in-memory: the working set is small, which keeps the repository
        // contract simple and the filter logic trivially testable.
        IEnumerable<TaskItem> filtered = tasks;

        if (caller.IsAdmin && !string.IsNullOrWhiteSpace(query.AssigneeId))
            filtered = filtered.Where(t => t.AssigneeId == query.AssigneeId);

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

        var result = filtered.ToList();
        var names = await ResolveAssigneeNamesAsync(result, ct);
        return result.Select(t => t.ToDto(NameFor(t, names))).ToList();
    }

    public async Task<TaskDto> GetByIdAsync(CurrentUser caller, Guid id, CancellationToken ct = default)
    {
        var task = await LoadAccessibleAsync(caller, id, ct);
        return task.ToDto(await ResolveAssigneeNameAsync(task, ct));
    }

    public async Task<TaskDto> CreateAsync(CurrentUser caller, CreateTaskDto dto, CancellationToken ct = default)
    {
        var task = dto.ToEntity();
        task.Id = Guid.NewGuid();
        task.UserId = caller.Id;

        // Only admins may target someone else; a regular user's task is assigned to themselves.
        var assigneeId = caller.IsAdmin && !string.IsNullOrWhiteSpace(dto.AssigneeId) ? dto.AssigneeId : caller.Id;
        await EnsureAssigneeExistsAsync(assigneeId, ct);
        task.AssigneeId = assigneeId;

        task.CreatedAt = DateTime.UtcNow;
        // New tasks append to the end of the assignee's list.
        task.SortOrder = await _repository.GetMaxSortOrderAsync(assigneeId!, ct) + 1;

        await _repository.AddAsync(task, ct);
        _logger.LogInformation("User {UserId} created task {TaskId} assigned to {AssigneeId}", caller.Id, task.Id, assigneeId);

        return task.ToDto(await ResolveAssigneeNameAsync(task, ct));
    }

    public async Task<TaskDto> UpdateAsync(CurrentUser caller, Guid id, UpdateTaskDto dto, CancellationToken ct = default)
    {
        var task = await LoadAccessibleAsync(caller, id, ct);

        task.ApplyUpdate(dto);
        task.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(task, ct);
        _logger.LogInformation("User {UserId} updated task {TaskId}", caller.Id, id);

        return task.ToDto(await ResolveAssigneeNameAsync(task, ct));
    }

    public async Task<TaskDto> SetCompletionAsync(CurrentUser caller, Guid id, bool isCompleted, CancellationToken ct = default)
    {
        var task = await LoadAccessibleAsync(caller, id, ct);

        task.SetCompletion(isCompleted);
        task.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(task, ct);
        return task.ToDto(await ResolveAssigneeNameAsync(task, ct));
    }

    public async Task<TaskDto> AssignAsync(CurrentUser caller, Guid id, string? assigneeId, CancellationToken ct = default)
    {
        if (!caller.IsAdmin)
            throw new NotFoundException(nameof(TaskItem), id); // hide existence from non-admins

        var task = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(TaskItem), id);

        await EnsureAssigneeExistsAsync(assigneeId, ct);

        if (task.AssigneeId != assigneeId)
        {
            task.AssigneeId = assigneeId;
            // Place the task at the end of the new assignee's list so ordering stays sensible.
            task.SortOrder = assigneeId is null ? task.SortOrder : await _repository.GetMaxSortOrderAsync(assigneeId, ct) + 1;
            task.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(task, ct);
            _logger.LogInformation("Admin {AdminId} assigned task {TaskId} to {AssigneeId}", caller.Id, id, assigneeId ?? "(unassigned)");
        }

        return task.ToDto(await ResolveAssigneeNameAsync(task, ct));
    }

    public async Task ReorderAsync(CurrentUser caller, ReorderTasksDto dto, CancellationToken ct = default)
    {
        // Reordering applies to the caller's own assigned list. (Admins reorder their own list too;
        // cross-user reordering is not a supported operation.)
        var tasks = await _repository.GetForAssigneeAsync(caller.Id, ct);
        var byId = tasks.ToDictionary(t => t.Id);

        // Assign SortOrder by the position of each id in the supplied order. Ids the caller does
        // not have (or that no longer exist) are ignored so a stale client cannot corrupt data.
        var changed = new List<TaskItem>();
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
            _logger.LogInformation("Reordered {Count} tasks for user {UserId}", changed.Count, caller.Id);
        }
    }

    public async Task DeleteAsync(CurrentUser caller, Guid id, CancellationToken ct = default)
    {
        var task = await LoadAccessibleAsync(caller, id, ct);

        await _repository.DeleteAsync(task, ct);
        _logger.LogInformation("User {UserId} deleted task {TaskId}", caller.Id, id);
    }

    // ----------------------------------------------------------------- helpers

    /// <summary>
    /// Loads a task and verifies the caller may act on it: admins may touch any task, regular
    /// users only tasks assigned to them. Throws NotFound (rather than Forbidden) so a caller
    /// cannot distinguish "exists but not yours" from "does not exist".
    /// </summary>
    private async Task<TaskItem> LoadAccessibleAsync(CurrentUser caller, Guid id, CancellationToken ct)
    {
        var task = await _repository.GetByIdAsync(id, ct);
        if (task is null || (!caller.IsAdmin && task.AssigneeId != caller.Id))
            throw new NotFoundException(nameof(TaskItem), id);
        return task;
    }

    /// <summary>Validates that a non-null assignee id refers to a real account.</summary>
    private async Task EnsureAssigneeExistsAsync(string? assigneeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(assigneeId))
            return;

        if (await _users.FindByIdAsync(assigneeId, ct) is null)
            throw new ValidationException(nameof(CreateTaskDto.AssigneeId), "The selected assignee does not exist.");
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveAssigneeNamesAsync(IReadOnlyCollection<TaskItem> tasks, CancellationToken ct)
    {
        if (tasks.Count == 0 || tasks.All(t => t.AssigneeId is null))
            return new Dictionary<string, string>();

        var all = await _users.GetAllAsync(ct);
        return all.ToDictionary(u => u.Id, u => u.DisplayName);
    }

    private static string? NameFor(TaskItem task, IReadOnlyDictionary<string, string> names) =>
        task.AssigneeId is not null && names.TryGetValue(task.AssigneeId, out var name) ? name : null;

    private async Task<string?> ResolveAssigneeNameAsync(TaskItem task, CancellationToken ct)
    {
        if (task.AssigneeId is null)
            return null;
        var user = await _users.FindByIdAsync(task.AssigneeId, ct);
        return user?.DisplayName;
    }
}

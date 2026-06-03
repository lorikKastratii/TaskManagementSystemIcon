using Microsoft.Extensions.Logging;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Application.Tasks.Mapping;
using TaskManager.Application.Users.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks.Services;

/// <summary>
/// Coordinates task use-cases: defaults, the Status/IsCompleted invariant, filtering and
/// drag-and-drop reordering. Persistence is delegated to <see cref="ITaskRepository"/> and
/// committed through <see cref="IUnitOfWork"/>; account lookups go through <see cref="IUserDirectory"/>.
///
/// The board is shared: every authenticated user may act on any task, and only an admin may
/// (re)assign a task to someone else.
/// </summary>
public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserDirectory _users;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository repository,
        IUnitOfWork unitOfWork,
        IUserDirectory users,
        ILogger<TaskService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _users = users;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(CurrentUser caller, TaskQueryParameters query, CancellationToken ct = default)
    {
        var tasks = await _repository.GetAllAsync(ct);
        var filtered = ApplyFilters(tasks, query).ToList();

        var names = await ResolveAssigneeNamesAsync(filtered, ct);
        return filtered.Select(t => t.ToDto(NameFor(t, names))).ToList();
    }

    public async Task<TaskDto> GetByIdAsync(CurrentUser caller, Guid id, CancellationToken ct = default)
    {
        var task = await LoadAccessibleAsync(id, ct);
        return task.ToDto(await ResolveAssigneeNameAsync(task, ct));
    }

    public async Task<TaskDto> CreateAsync(CurrentUser caller, CreateTaskDto dto, CancellationToken ct = default)
    {
        var assigneeId = await ResolveCreateAssigneeAsync(caller, dto, ct);

        var task = dto.ToEntity();
        task.Id = Guid.NewGuid();
        task.UserId = caller.Id;
        task.AssigneeId = assigneeId;
        task.CreatedAt = DateTime.UtcNow;
        task.SortOrder = await _repository.GetMaxSortOrderAsync(assigneeId!, ct) + 1;

        _repository.Add(task);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("User {UserId} created task {TaskId} assigned to {AssigneeId}", caller.Id, task.Id, assigneeId);

        return task.ToDto(await ResolveAssigneeNameAsync(task, ct));
    }

    public async Task<TaskDto> UpdateAsync(CurrentUser caller, Guid id, UpdateTaskDto dto, CancellationToken ct = default)
    {
        var task = await LoadAccessibleAsync(id, ct);

        task.ApplyUpdate(dto);
        task.UpdatedAt = DateTime.UtcNow;

        await PersistAsync(task, ct);
        _logger.LogInformation("User {UserId} updated task {TaskId}", caller.Id, id);

        return task.ToDto(await ResolveAssigneeNameAsync(task, ct));
    }

    public async Task<TaskDto> SetCompletionAsync(CurrentUser caller, Guid id, bool isCompleted, CancellationToken ct = default)
    {
        var task = await LoadAccessibleAsync(id, ct);

        task.SetCompletion(isCompleted);
        task.UpdatedAt = DateTime.UtcNow;

        await PersistAsync(task, ct);
        return task.ToDto(await ResolveAssigneeNameAsync(task, ct));
    }

    public async Task<TaskDto> AssignAsync(CurrentUser caller, Guid id, string? assigneeId, CancellationToken ct = default)
    {
        // Hide existence from non-admins rather than returning 403.
        if (!caller.IsAdmin)
        {
            throw new NotFoundException(nameof(TaskItem), id);
        }

        var task = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(TaskItem), id);

        await EnsureAssigneeExistsAsync(assigneeId, ct);

        if (task.AssigneeId != assigneeId)
        {
            await ReassignAsync(task, assigneeId, ct);
            _logger.LogInformation("Admin {AdminId} assigned task {TaskId} to {AssigneeId}", caller.Id, id, assigneeId ?? "(unassigned)");
        }

        return task.ToDto(await ResolveAssigneeNameAsync(task, ct));
    }

    public async Task ReorderAsync(CurrentUser caller, ReorderTasksDto dto, CancellationToken ct = default)
    {
        var tasks = await _repository.GetAllAsync(ct);
        var changed = ApplyOrdering(tasks, dto.OrderedTaskIds);
        if (changed.Count == 0)
        {
            return;
        }

        _repository.UpdateRange(changed);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Reordered {Count} tasks for user {UserId}", changed.Count, caller.Id);
    }

    public async Task DeleteAsync(CurrentUser caller, Guid id, CancellationToken ct = default)
    {
        var task = await LoadAccessibleAsync(id, ct);

        _repository.Remove(task);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("User {UserId} deleted task {TaskId}", caller.Id, id);
    }

    private static IEnumerable<TaskItem> ApplyFilters(IEnumerable<TaskItem> tasks, TaskQueryParameters query)
    {
        if (!string.IsNullOrWhiteSpace(query.AssigneeId))
        {
            tasks = tasks.Where(t => t.AssigneeId == query.AssigneeId);
        }

        if (query.Status.HasValue)
        {
            tasks = tasks.Where(t => t.Status == query.Status.Value);
        }

        if (query.Priority.HasValue)
        {
            tasks = tasks.Where(t => t.Priority == query.Priority.Value);
        }

        if (query.IsCompleted.HasValue)
        {
            tasks = tasks.Where(t => t.IsCompleted == query.IsCompleted.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            tasks = tasks.Where(t => MatchesSearch(t, term));
        }

        return tasks;
    }

    private static bool MatchesSearch(TaskItem task, string term)
    {
        return task.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
            || (task.Description is not null && task.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private async Task PersistAsync(TaskItem task, CancellationToken ct)
    {
        _repository.Update(task);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>Moves a task to a new assignee, appending it to the end of their list.</summary>
    private async Task ReassignAsync(TaskItem task, string? assigneeId, CancellationToken ct)
    {
        task.AssigneeId = assigneeId;
        if (assigneeId is not null)
        {
            task.SortOrder = await _repository.GetMaxSortOrderAsync(assigneeId, ct) + 1;
        }

        task.UpdatedAt = DateTime.UtcNow;
        await PersistAsync(task, ct);
    }

    /// <summary>
    /// Resolves the assignee for a new task. Only admins may target someone else; a regular
    /// user's task is always assigned to themselves.
    /// </summary>
    private async Task<string?> ResolveCreateAssigneeAsync(CurrentUser caller, CreateTaskDto dto, CancellationToken ct)
    {
        var assigneeId = caller.IsAdmin && !string.IsNullOrWhiteSpace(dto.AssigneeId) ? dto.AssigneeId : caller.Id;
        await EnsureAssigneeExistsAsync(assigneeId, ct);
        return assigneeId;
    }

    /// <summary>Reassigns SortOrder by position; ids that no longer exist are ignored.</summary>
    private static List<TaskItem> ApplyOrdering(IReadOnlyList<TaskItem> tasks, IReadOnlyList<Guid> orderedIds)
    {
        var byId = tasks.ToDictionary(t => t.Id);
        var changed = new List<TaskItem>();

        for (var index = 0; index < orderedIds.Count; index++)
        {
            if (byId.TryGetValue(orderedIds[index], out var task) && task.SortOrder != index)
            {
                task.SortOrder = index;
                task.UpdatedAt = DateTime.UtcNow;
                changed.Add(task);
            }
        }

        return changed;
    }

    private async Task<TaskItem> LoadAccessibleAsync(Guid id, CancellationToken ct)
    {
        return await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(TaskItem), id);
    }

    private async Task EnsureAssigneeExistsAsync(string? assigneeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(assigneeId))
        {
            return;
        }

        if (await _users.FindByIdAsync(assigneeId, ct) is null)
        {
            throw new ValidationException(nameof(CreateTaskDto.AssigneeId), "The selected assignee does not exist.");
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveAssigneeNamesAsync(IReadOnlyCollection<TaskItem> tasks, CancellationToken ct)
    {
        if (tasks.Count == 0 || tasks.All(t => t.AssigneeId is null))
        {
            return new Dictionary<string, string>();
        }

        var all = await _users.GetAllAsync(ct);
        return all.ToDictionary(u => u.Id, u => u.DisplayName);
    }

    private static string? NameFor(TaskItem task, IReadOnlyDictionary<string, string> names)
    {
        return task.AssigneeId is not null && names.TryGetValue(task.AssigneeId, out var name) ? name : null;
    }

    private async Task<string?> ResolveAssigneeNameAsync(TaskItem task, CancellationToken ct)
    {
        if (task.AssigneeId is null)
        {
            return null;
        }

        var user = await _users.FindByIdAsync(task.AssigneeId, ct);
        return user?.DisplayName;
    }
}

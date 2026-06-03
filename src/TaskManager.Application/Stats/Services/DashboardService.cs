using TaskManager.Application.Stats.Dtos;
using TaskManager.Application.Stats.Interfaces;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Application.Users.Dtos;
using TaskManager.Application.Users.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Stats.Services;

/// <summary>
/// Builds <see cref="DashboardStatsDto"/> from the shared task board. Pure aggregation over the
/// repository and user directory, so it stays unit-testable with no infrastructure dependencies.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ITaskRepository _repository;
    private readonly IUserDirectory _users;

    public DashboardService(ITaskRepository repository, IUserDirectory users)
    {
        _repository = repository;
        _users = users;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var tasks = await _repository.GetAllAsync(ct);
        var users = await _users.GetAllAsync(ct);

        var completed = tasks.Count(t => t.IsCompleted);

        return new DashboardStatsDto
        {
            TotalTasks = tasks.Count,
            CompletedTasks = completed,
            ActiveTasks = tasks.Count - completed,
            OverdueTasks = CountOverdue(tasks),
            UnassignedTasks = tasks.Count(t => t.AssigneeId is null),
            CompletionRate = CompletionRate(completed, tasks.Count),
            TasksByStatus = CountBy(tasks, Enum.GetValues<TaskItemStatus>(), t => t.Status),
            TasksByPriority = CountBy(tasks, Enum.GetValues<TaskPriority>(), t => t.Priority),
            PerUser = BuildPerUser(tasks, users),
        };
    }

    private static int CountOverdue(IEnumerable<TaskItem> tasks)
    {
        var now = DateTime.UtcNow;
        return tasks.Count(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value < now);
    }

    private static double CompletionRate(int completed, int total)
    {
        return total == 0 ? 0 : Math.Round((double)completed / total, 4);
    }

    /// <summary>
    /// Counts tasks per enum value, seeding every value to zero so the dashboard always shows
    /// the full set. Keyed by the string enum name to match how the API serialises enums.
    /// </summary>
    private static IReadOnlyDictionary<string, int> CountBy<TKey>(
        IEnumerable<TaskItem> tasks,
        IEnumerable<TKey> allValues,
        Func<TaskItem, TKey> selector)
        where TKey : notnull
    {
        var counts = allValues.ToDictionary(value => value.ToString()!, _ => 0);
        foreach (var task in tasks)
        {
            counts[selector(task).ToString()!]++;
        }

        return counts;
    }

    private static IReadOnlyList<UserTaskStatsDto> BuildPerUser(
        IReadOnlyList<TaskItem> tasks,
        IReadOnlyList<UserSummaryDto> users)
    {
        return users
            .Select(user => TallyFor(user, tasks))
            .OrderByDescending(u => u.Completed)
            .ThenByDescending(u => u.Assigned)
            .ThenBy(u => u.DisplayName)
            .ToList();
    }

    private static UserTaskStatsDto TallyFor(UserSummaryDto user, IEnumerable<TaskItem> tasks)
    {
        var assigned = tasks.Where(t => t.AssigneeId == user.Id).ToList();
        var completed = assigned.Count(t => t.IsCompleted);

        return new UserTaskStatsDto
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Assigned = assigned.Count,
            Completed = completed,
            Active = assigned.Count - completed,
        };
    }
}

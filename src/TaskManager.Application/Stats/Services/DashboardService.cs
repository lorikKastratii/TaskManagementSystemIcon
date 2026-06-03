using TaskManager.Application.Stats.Dtos;
using TaskManager.Application.Stats.Interfaces;
using TaskManager.Application.Tasks.Interfaces;
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
        var now = DateTime.UtcNow;

        var total = tasks.Count;
        var completed = tasks.Count(t => t.IsCompleted);

        // Initialise every enum bucket to zero so the dashboard always shows the full set.
        var byStatus = Enum.GetValues<TaskItemStatus>().ToDictionary(s => s.ToString(), _ => 0);
        var byPriority = Enum.GetValues<TaskPriority>().ToDictionary(p => p.ToString(), _ => 0);
        foreach (var task in tasks)
        {
            byStatus[task.Status.ToString()]++;
            byPriority[task.Priority.ToString()]++;
        }

        var perUser = users
            .Select(u =>
            {
                var assigned = tasks.Where(t => t.AssigneeId == u.Id).ToList();
                var done = assigned.Count(t => t.IsCompleted);
                return new UserTaskStatsDto
                {
                    UserId = u.Id,
                    DisplayName = u.DisplayName,
                    Assigned = assigned.Count,
                    Completed = done,
                    Active = assigned.Count - done,
                };
            })
            .OrderByDescending(u => u.Completed)
            .ThenByDescending(u => u.Assigned)
            .ThenBy(u => u.DisplayName)
            .ToList();

        return new DashboardStatsDto
        {
            TotalTasks = total,
            CompletedTasks = completed,
            ActiveTasks = total - completed,
            OverdueTasks = tasks.Count(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value < now),
            UnassignedTasks = tasks.Count(t => t.AssigneeId is null),
            CompletionRate = total == 0 ? 0 : Math.Round((double)completed / total, 4),
            TasksByStatus = byStatus,
            TasksByPriority = byPriority,
            PerUser = perUser,
        };
    }
}

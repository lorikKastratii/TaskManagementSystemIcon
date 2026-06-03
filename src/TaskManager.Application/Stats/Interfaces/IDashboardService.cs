using TaskManager.Application.Stats.Dtos;

namespace TaskManager.Application.Stats.Interfaces;

/// <summary>Computes aggregate task statistics for the dashboard.</summary>
public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default);
}

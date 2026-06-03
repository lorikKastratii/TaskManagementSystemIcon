using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Stats.Dtos;
using TaskManager.Application.Stats.Interfaces;

namespace TaskManager.API.Controllers;

/// <summary>
/// Read-only aggregate statistics for the dashboard. The board is shared, so the figures span
/// every task in the system and are available to any authenticated user.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    /// <summary>Returns task counts, status/priority breakdowns and per-user tallies.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatsDto>> GetStats(CancellationToken ct)
    {
        var stats = await _dashboard.GetStatsAsync(ct);
        return Ok(stats);
    }
}

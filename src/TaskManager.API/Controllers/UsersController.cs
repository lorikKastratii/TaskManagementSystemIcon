using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Users.Dtos;
using TaskManager.Application.Users.Interfaces;

namespace TaskManager.API.Controllers;

/// <summary>
/// Admin-only directory of accounts, used to populate the "assign to" picker and the assignee
/// filter in the UI. Restricted to the Admin role so regular users cannot enumerate accounts.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserDirectory _users;

    public UsersController(IUserDirectory users)
    {
        _users = users;
    }

    /// <summary>Lists every account that a task can be assigned to.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> GetUsers(CancellationToken ct)
    {
        return Ok(await _users.GetAllAsync(ct));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Common;
using TaskManager.Application.Auth.Dtos;
using TaskManager.Application.Auth.Interfaces;
using TaskManager.Infrastructure.Identity;

namespace TaskManager.API.Controllers;

/// <summary>
/// Registration and login endpoints. On success a JWT is returned that the client attaches as a
/// Bearer token on subsequent requests. The token carries the user id used to scope task access.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthController(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    /// <summary>Registers a new user and returns an access token.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
        {
            ModelState.AddModelError(nameof(dto.Email), "An account with this email already exists.");
            return ValidationProblem(ModelState);
        }

        var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return ValidationProblem(ModelState);
        }

        return Ok(BuildAuthResponse(user));
    }

    /// <summary>Authenticates an existing user and returns an access token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            // Same response for unknown email and wrong password to avoid user enumeration.
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail = "Invalid email or password."
            });
        }

        return Ok(BuildAuthResponse(user));
    }

    /// <summary>Returns the currently authenticated user's basic profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetCurrentUser()
    {
        return Ok(new
        {
            id = User.GetUserId(),
            email = User.Identity?.Name
        });
    }

    private AuthResponseDto BuildAuthResponse(ApplicationUser user)
    {
        var (token, expiresAt) = _tokenService.CreateToken(user.Id, user.Email!);
        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email!
        };
    }
}

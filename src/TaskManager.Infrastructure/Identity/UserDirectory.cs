using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Users.Dtos;
using TaskManager.Application.Users.Interfaces;

namespace TaskManager.Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity-backed implementation of <see cref="IUserDirectory"/>. Projects
/// <see cref="ApplicationUser"/> records into the Application's <see cref="UserSummaryDto"/>,
/// including whether each account holds the Admin role.
/// </summary>
public class UserDirectory : IUserDirectory
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserDirectory(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _userManager.Users
            .OrderBy(u => u.DisplayName ?? u.Email)
            .ToListAsync(ct);

        var result = new List<UserSummaryDto>(users.Count);
        foreach (var user in users)
        {
            result.Add(await ToSummaryAsync(user));
        }
        return result;
    }

    public async Task<UserSummaryDto?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user is null ? null : await ToSummaryAsync(user);
    }

    private async Task<UserSummaryDto> ToSummaryAsync(ApplicationUser user)
    {
        var isAdmin = await _userManager.IsInRoleAsync(user, IdentitySeeder.AdminRole);
        return new UserSummaryDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email ?? user.Id : user.DisplayName,
            IsAdmin = isAdmin
        };
    }
}

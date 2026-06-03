using TaskManager.Application.Users.Dtos;

namespace TaskManager.Application.Users.Interfaces;

/// <summary>
/// Read-only access to the set of accounts in the system, abstracting ASP.NET Identity away
/// from the Application layer. Used to list assignable people (admin) and to resolve assignee
/// display names when projecting tasks to DTOs. Implemented in the Infrastructure layer.
/// </summary>
public interface IUserDirectory
{
    /// <summary>Returns every account, ordered by display name then email.</summary>
    Task<IReadOnlyList<UserSummaryDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a single account by id, or null if it does not exist.</summary>
    Task<UserSummaryDto?> FindByIdAsync(string id, CancellationToken ct = default);
}

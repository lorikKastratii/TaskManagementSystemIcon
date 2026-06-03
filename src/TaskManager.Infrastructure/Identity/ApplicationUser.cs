using Microsoft.AspNetCore.Identity;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Identity;

/// <summary>
/// Application user backed by ASP.NET Core Identity. Owns a collection of tasks so that
/// data access can be scoped per user. Lives in Infrastructure because Identity is a
/// persistence concern and the Domain must stay free of framework dependencies.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Friendly display name shown in the UI (e.g. "test1"). Falls back to the email when unset.</summary>
    public string? DisplayName { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}

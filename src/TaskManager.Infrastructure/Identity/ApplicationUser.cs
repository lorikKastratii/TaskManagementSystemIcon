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
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}

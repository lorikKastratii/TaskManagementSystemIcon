using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TaskManager.Infrastructure.Identity;

/// <summary>
/// Idempotently seeds the role set and the initial accounts the system ships with:
/// a dedicated admin plus three assignable people (test1–test3). Safe to run on every
/// startup — existing roles/users are left untouched.
/// </summary>
public static class IdentitySeeder
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";

    /// <summary>The accounts created on first run. Passwords satisfy the configured password policy.</summary>
    private static readonly SeedUser[] SeedUsers =
    [
        new("admin@taskmanager.local", "admin",  "Admin123!", AdminRole),
        new("test1@taskmanager.local", "test1",  "Test123!",  UserRole),
        new("test2@taskmanager.local", "test2",  "Test123!",  UserRole),
        new("test3@taskmanager.local", "test3",  "Test123!",  UserRole),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        foreach (var role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Seeded role {Role}", role);
            }
        }

        foreach (var seed in SeedUsers)
        {
            if (await userManager.FindByEmailAsync(seed.Email) is not null)
            {
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = seed.Email,
                Email = seed.Email,
                EmailConfirmed = true,
                DisplayName = seed.DisplayName
            };

            var created = await userManager.CreateAsync(user, seed.Password);
            if (!created.Succeeded)
            {
                logger.LogError("Failed to seed user {Email}: {Errors}", seed.Email,
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, seed.Role);
            logger.LogInformation("Seeded user {Email} ({DisplayName}) in role {Role}", seed.Email, seed.DisplayName, seed.Role);
        }
    }

    private sealed record SeedUser(string Email, string DisplayName, string Password, string Role);
}

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Identity;

namespace TaskManager.Infrastructure.Data;

/// <summary>
/// EF Core context combining ASP.NET Identity tables with the application's task data.
/// Entity mappings are applied from IEntityTypeConfiguration classes in this assembly,
/// and a one-to-many User → Tasks relationship enforces ownership with cascade delete.
/// </summary>
public class TaskDbContext : IdentityDbContext<ApplicationUser>
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(TaskDbContext).Assembly);

        builder.Entity<ApplicationUser>()
            .Property(u => u.DisplayName)
            .HasMaxLength(100);

        // A user owns (created) many tasks; deleting a user removes the tasks they created.
        // AssigneeId is intentionally a plain column (no cascade) so removing a user does not
        // delete tasks merely assigned to them.
        builder.Entity<ApplicationUser>()
            .HasMany(u => u.Tasks)
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

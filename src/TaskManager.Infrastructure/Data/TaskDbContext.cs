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

        // A user owns many tasks; deleting a user removes their tasks.
        builder.Entity<ApplicationUser>()
            .HasMany(u => u.Tasks)
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

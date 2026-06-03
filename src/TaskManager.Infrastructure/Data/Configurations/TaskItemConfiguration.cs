using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core mapping for <see cref="TaskItem"/>. Keeping configuration in a dedicated class
/// (rather than inline in OnModelCreating) keeps the DbContext small and the schema explicit.
/// </summary>
public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        // Persist enums as readable strings rather than opaque ints.
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.UserId)
            .IsRequired();

        // Nullable: a task may be unassigned. Length matches Identity's string key.
        builder.Property(t => t.AssigneeId)
            .HasMaxLength(450);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        // Most common query is "all tasks for an assignee, ordered" — index accordingly.
        builder.HasIndex(t => new { t.AssigneeId, t.SortOrder });
        builder.HasIndex(t => new { t.UserId, t.Status });
    }
}

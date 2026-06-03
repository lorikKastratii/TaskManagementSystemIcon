using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManager.Infrastructure.Data;
using TaskManager.Infrastructure.Repositories;
using TaskManager.UnitTests.Builders;

namespace TaskManager.UnitTests.Tasks;

/// <summary>
/// Integration-style tests for <see cref="TaskRepository"/> against the EF Core InMemory
/// provider. Primary purpose: prove that every read is scoped to the owning user so data
/// cannot leak between accounts.
/// </summary>
public class TaskRepositoryTests
{
    private static TaskDbContext NewContext()
    {
        // Unique database name per context so tests are isolated from one another.
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase($"repo-tests-{Guid.NewGuid()}")
            .Options;
        return new TaskDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyTasksOwnedByUser()
    {
        await using var db = NewContext();
        db.Tasks.AddRange(
            TaskItemBuilder.ForUser("alice").Build(),
            TaskItemBuilder.ForUser("alice").Build(),
            TaskItemBuilder.ForUser("bob").Build());
        await db.SaveChangesAsync();

        var repo = new TaskRepository(db);
        var aliceTasks = await repo.GetAllAsync("alice");

        aliceTasks.Should().HaveCount(2);
        aliceTasks.Should().OnlyContain(t => t.UserId == "alice");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenTaskBelongsToAnotherUser()
    {
        await using var db = NewContext();
        var bobsTask = TaskItemBuilder.ForUser("bob").Build();
        db.Tasks.Add(bobsTask);
        await db.SaveChangesAsync();

        var repo = new TaskRepository(db);
        var result = await repo.GetByIdAsync("alice", bobsTask.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMaxSortOrderAsync_ShouldReturnZero_WhenUserHasNoTasks()
    {
        await using var db = NewContext();
        var repo = new TaskRepository(db);

        var max = await repo.GetMaxSortOrderAsync("nobody");

        max.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_ShouldOrderBySortOrder()
    {
        await using var db = NewContext();
        db.Tasks.AddRange(
            TaskItemBuilder.ForUser("alice").WithSortOrder(2).WithTitle("second").Build(),
            TaskItemBuilder.ForUser("alice").WithSortOrder(0).WithTitle("first").Build());
        await db.SaveChangesAsync();

        var repo = new TaskRepository(db);
        var tasks = await repo.GetAllAsync("alice");

        tasks.Select(t => t.Title).Should().ContainInOrder("first", "second");
    }
}

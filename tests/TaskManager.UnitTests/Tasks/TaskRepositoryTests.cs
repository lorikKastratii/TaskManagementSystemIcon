using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManager.Infrastructure.Data;
using TaskManager.Infrastructure.Repositories;
using TaskManager.UnitTests.Builders;

namespace TaskManager.UnitTests.Tasks;

/// <summary>
/// Integration-style tests for <see cref="TaskRepository"/> against the EF Core InMemory
/// provider. Primary purpose: prove that a regular user's reads are scoped to the tasks
/// assigned to them, while the admin read returns everything.
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
    public async Task GetForAssigneeAsync_ShouldReturnOnlyTasksAssignedToUser()
    {
        await using var db = NewContext();
        db.Tasks.AddRange(
            TaskItemBuilder.ForUser("alice").Build(),
            TaskItemBuilder.ForUser("alice").Build(),
            TaskItemBuilder.ForUser("bob").Build());
        await db.SaveChangesAsync();

        var repo = new TaskRepository(db);
        var aliceTasks = await repo.GetForAssigneeAsync("alice");

        aliceTasks.Should().HaveCount(2);
        aliceTasks.Should().OnlyContain(t => t.AssigneeId == "alice");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEveryTask_RegardlessOfAssignee()
    {
        await using var db = NewContext();
        db.Tasks.AddRange(
            TaskItemBuilder.ForUser("alice").Build(),
            TaskItemBuilder.ForUser("bob").Build());
        await db.SaveChangesAsync();

        var repo = new TaskRepository(db);
        var all = await repo.GetAllAsync();

        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTask_RegardlessOfAssignee()
    {
        await using var db = NewContext();
        var bobsTask = TaskItemBuilder.ForUser("bob").Build();
        db.Tasks.Add(bobsTask);
        await db.SaveChangesAsync();

        var repo = new TaskRepository(db);
        var result = await repo.GetByIdAsync(bobsTask.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(bobsTask.Id);
    }

    [Fact]
    public async Task GetMaxSortOrderAsync_ShouldReturnZero_WhenAssigneeHasNoTasks()
    {
        await using var db = NewContext();
        var repo = new TaskRepository(db);

        var max = await repo.GetMaxSortOrderAsync("nobody");

        max.Should().Be(0);
    }

    [Fact]
    public async Task GetForAssigneeAsync_ShouldOrderBySortOrder()
    {
        await using var db = NewContext();
        db.Tasks.AddRange(
            TaskItemBuilder.ForUser("alice").WithSortOrder(2).WithTitle("second").Build(),
            TaskItemBuilder.ForUser("alice").WithSortOrder(0).WithTitle("first").Build());
        await db.SaveChangesAsync();

        var repo = new TaskRepository(db);
        var tasks = await repo.GetForAssigneeAsync("alice");

        tasks.Select(t => t.Title).Should().ContainInOrder("first", "second");
    }
}

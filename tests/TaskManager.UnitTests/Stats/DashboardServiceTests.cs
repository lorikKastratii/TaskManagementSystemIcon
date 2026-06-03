using FluentAssertions;
using Moq;
using TaskManager.Application.Stats.Services;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Application.Users.Dtos;
using TaskManager.Application.Users.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.UnitTests.Builders;

namespace TaskManager.UnitTests.Stats;

/// <summary>
/// Unit tests for <see cref="DashboardService"/>. The repository and user directory are mocked so
/// the tests focus purely on the aggregation logic.
/// </summary>
public class DashboardServiceTests
{
    private readonly Mock<ITaskRepository> _repository = new();
    private readonly Mock<IUserDirectory> _users = new();
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _users.Setup(u => u.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserSummaryDto>());
        _sut = new DashboardService(_repository.Object, _users.Object);
    }

    private void HaveTasks(params TaskItem[] tasks) =>
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

    [Fact]
    public async Task GetStatsAsync_ShouldReturnZeroes_WhenNoTasks()
    {
        HaveTasks();

        var stats = await _sut.GetStatsAsync();

        stats.TotalTasks.Should().Be(0);
        stats.CompletionRate.Should().Be(0);
        // Every enum bucket is present even with no tasks.
        stats.TasksByStatus.Should().ContainKeys("Todo", "InProgress", "InReview", "Done");
        stats.TasksByPriority.Should().ContainKeys("Low", "Medium", "High");
    }

    [Fact]
    public async Task GetStatsAsync_ShouldCountCompletionAndBreakdowns()
    {
        HaveTasks(
            TaskItemBuilder.ForUser("u1").WithPriority(TaskPriority.High).WithStatus(TaskItemStatus.InProgress).Build(),
            TaskItemBuilder.ForUser("u1").WithPriority(TaskPriority.Low).Completed().Build(),
            TaskItemBuilder.ForUser("u1").WithPriority(TaskPriority.Low).Build());

        var stats = await _sut.GetStatsAsync();

        stats.TotalTasks.Should().Be(3);
        stats.CompletedTasks.Should().Be(1);
        stats.ActiveTasks.Should().Be(2);
        stats.CompletionRate.Should().BeApproximately(0.3333, 0.0001);
        stats.TasksByPriority["Low"].Should().Be(2);
        stats.TasksByPriority["High"].Should().Be(1);
        stats.TasksByStatus["Done"].Should().Be(1); // completing moves the task to Done
    }

    [Fact]
    public async Task GetStatsAsync_ShouldCountOverdue_OnlyForIncompletePastDueTasks()
    {
        HaveTasks(
            TaskItemBuilder.ForUser("u1").WithDueDate(DateTime.UtcNow.AddDays(-1)).Build(),          // overdue
            TaskItemBuilder.ForUser("u1").WithDueDate(DateTime.UtcNow.AddDays(-1)).Completed().Build(), // past due but done
            TaskItemBuilder.ForUser("u1").WithDueDate(DateTime.UtcNow.AddDays(1)).Build());           // future

        var stats = await _sut.GetStatsAsync();

        stats.OverdueTasks.Should().Be(1);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldTallyPerUser_OrderedByCompleted()
    {
        _users.Setup(u => u.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new UserSummaryDto { Id = "u1", DisplayName = "Alice" },
            new UserSummaryDto { Id = "u2", DisplayName = "Bob" },
        });
        HaveTasks(
            TaskItemBuilder.ForUser("u1").Build(),
            TaskItemBuilder.ForUser("u2").Completed().Build(),
            TaskItemBuilder.ForUser("u2").Build());

        var stats = await _sut.GetStatsAsync();

        // Bob has a completion, so he sorts first.
        stats.PerUser.Should().HaveCount(2);
        stats.PerUser[0].DisplayName.Should().Be("Bob");
        stats.PerUser[0].Assigned.Should().Be(2);
        stats.PerUser[0].Completed.Should().Be(1);
        stats.PerUser[0].Active.Should().Be(1);
        stats.PerUser[1].DisplayName.Should().Be("Alice");
        stats.PerUser[1].Completed.Should().Be(0);
    }
}

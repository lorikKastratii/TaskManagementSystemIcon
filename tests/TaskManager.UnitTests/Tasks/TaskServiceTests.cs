using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Application.Tasks.Services;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.UnitTests.Builders;

namespace TaskManager.UnitTests.Tasks;

/// <summary>
/// Unit tests for <see cref="TaskService"/>. The repository is mocked so the tests focus on
/// orchestration logic: ownership enforcement, defaults, the Status/IsCompleted invariant,
/// filtering and reordering.
/// </summary>
public class TaskServiceTests
{
    private const string UserId = "user-1";

    private readonly Mock<ITaskRepository> _repository = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(_repository.Object, NullLogger<TaskService>.Instance);
    }

    // ---------------------------------------------------------------- GetTasks (filtering)

    [Fact]
    public async Task GetTasksAsync_ShouldReturnAllTasks_WhenNoFilterSupplied()
    {
        _repository.Setup(r => r.GetAllAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                TaskItemBuilder.ForUser(UserId).WithStatus(TaskItemStatus.Todo).Build(),
                TaskItemBuilder.ForUser(UserId).WithStatus(TaskItemStatus.Done).Completed().Build()
            });

        var result = await _sut.GetTasksAsync(UserId, new TaskQueryParameters());

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTasksAsync_ShouldFilterByStatus()
    {
        _repository.Setup(r => r.GetAllAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                TaskItemBuilder.ForUser(UserId).WithStatus(TaskItemStatus.Todo).Build(),
                TaskItemBuilder.ForUser(UserId).WithStatus(TaskItemStatus.InProgress).Build()
            });

        var result = await _sut.GetTasksAsync(UserId, new TaskQueryParameters { Status = TaskItemStatus.InProgress });

        result.Should().ContainSingle()
            .Which.Status.Should().Be(TaskItemStatus.InProgress);
    }

    [Fact]
    public async Task GetTasksAsync_ShouldFilterBySearchTerm_CaseInsensitively()
    {
        _repository.Setup(r => r.GetAllAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                TaskItemBuilder.ForUser(UserId).WithTitle("Buy Milk").Build(),
                TaskItemBuilder.ForUser(UserId).WithTitle("Write report").Build()
            });

        var result = await _sut.GetTasksAsync(UserId, new TaskQueryParameters { Search = "milk" });

        result.Should().ContainSingle()
            .Which.Title.Should().Be("Buy Milk");
    }

    // ---------------------------------------------------------------- GetById

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenTaskMissing()
    {
        _repository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var act = () => _sut.GetByIdAsync(UserId, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------------------------------------------------------------- Create

    [Fact]
    public async Task CreateAsync_ShouldAssignOwnerIdAndAppendSortOrder()
    {
        _repository.Setup(r => r.GetMaxSortOrderAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        TaskItem? saved = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => saved = t)
            .Returns(Task.CompletedTask);

        var dto = new CreateTaskDto { Title = "New", Priority = TaskPriority.High };

        var result = await _sut.CreateAsync(UserId, dto);

        saved.Should().NotBeNull();
        saved!.UserId.Should().Be(UserId);
        saved.SortOrder.Should().Be(5); // max + 1
        saved.Id.Should().NotBeEmpty();
        result.Title.Should().Be("New");
        result.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task CreateAsync_ShouldMarkCompleted_WhenStatusIsDone()
    {
        _repository.Setup(r => r.GetMaxSortOrderAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repository.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(UserId, new CreateTaskDto { Title = "Done one", Status = TaskItemStatus.Done });

        result.IsCompleted.Should().BeTrue();
    }

    // ---------------------------------------------------------------- Update (partial)

    [Fact]
    public async Task UpdateAsync_ShouldOnlyChangeSuppliedFields_AndSetUpdatedAt()
    {
        var existing = TaskItemBuilder.ForUser(UserId)
            .WithTitle("Original")
            .WithPriority(TaskPriority.Low)
            .Build();
        _repository.Setup(r => r.GetByIdAsync(UserId, existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repository.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(UserId, existing.Id, new UpdateTaskDto { Title = "Renamed" });

        result.Title.Should().Be("Renamed");
        result.Priority.Should().Be(TaskPriority.Low); // unchanged
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFound_WhenTaskMissing()
    {
        _repository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var act = () => _sut.UpdateAsync(UserId, Guid.NewGuid(), new UpdateTaskDto { Title = "x" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------------------------------------------------------------- SetCompletion

    [Fact]
    public async Task SetCompletionAsync_ShouldMoveTaskToDone_WhenCompleted()
    {
        var task = TaskItemBuilder.ForUser(UserId).WithStatus(TaskItemStatus.InProgress).Build();
        _repository.Setup(r => r.GetByIdAsync(UserId, task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _repository.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.SetCompletionAsync(UserId, task.Id, true);

        result.IsCompleted.Should().BeTrue();
        result.Status.Should().Be(TaskItemStatus.Done);
    }

    [Fact]
    public async Task SetCompletionAsync_ShouldReopenToTodo_WhenUncompletingDoneTask()
    {
        var task = TaskItemBuilder.ForUser(UserId).WithStatus(TaskItemStatus.Done).Completed().Build();
        _repository.Setup(r => r.GetByIdAsync(UserId, task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _repository.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.SetCompletionAsync(UserId, task.Id, false);

        result.IsCompleted.Should().BeFalse();
        result.Status.Should().Be(TaskItemStatus.Todo);
    }

    // ---------------------------------------------------------------- Reorder

    [Fact]
    public async Task ReorderAsync_ShouldAssignSortOrderByPosition_AndIgnoreUnknownIds()
    {
        var a = TaskItemBuilder.ForUser(UserId).WithSortOrder(0).Build();
        var b = TaskItemBuilder.ForUser(UserId).WithSortOrder(1).Build();
        _repository.Setup(r => r.GetAllAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { a, b });
        IEnumerable<TaskItem>? updated = null;
        _repository.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<TaskItem>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TaskItem>, CancellationToken>((t, _) => updated = t.ToList())
            .Returns(Task.CompletedTask);

        // New order: b first, a second, plus a stale/unknown id that must be ignored.
        await _sut.ReorderAsync(UserId, new ReorderTasksDto { OrderedTaskIds = new[] { b.Id, a.Id, Guid.NewGuid() } });

        b.SortOrder.Should().Be(0);
        a.SortOrder.Should().Be(1);
        updated.Should().NotBeNull();
        updated!.Should().HaveCount(2);
    }

    // ---------------------------------------------------------------- Delete

    [Fact]
    public async Task DeleteAsync_ShouldThrowNotFound_WhenTaskMissing()
    {
        _repository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var act = () => _sut.DeleteAsync(UserId, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.DeleteAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTask_WhenOwnedByUser()
    {
        var task = TaskItemBuilder.ForUser(UserId).Build();
        _repository.Setup(r => r.GetByIdAsync(UserId, task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _repository.Setup(r => r.DeleteAsync(task, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(UserId, task.Id);

        _repository.Verify(r => r.DeleteAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }
}

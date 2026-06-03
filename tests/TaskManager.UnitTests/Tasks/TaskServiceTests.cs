using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Application.Tasks.Services;
using TaskManager.Application.Users.Dtos;
using TaskManager.Application.Users.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.UnitTests.Builders;

namespace TaskManager.UnitTests.Tasks;

/// <summary>
/// Unit tests for <see cref="TaskService"/>. The repository and user directory are mocked so the
/// tests focus on orchestration logic: assignment-based access control, defaults, the
/// Status/IsCompleted invariant, filtering, reassignment and reordering.
/// </summary>
public class TaskServiceTests
{
    private const string UserId = "user-1";
    private const string OtherId = "user-2";

    private static readonly CurrentUser User = new(UserId, IsAdmin: false);
    private static readonly CurrentUser Admin = new("admin-1", IsAdmin: true);

    private readonly Mock<ITaskRepository> _repository = new();
    private readonly Mock<IUserDirectory> _users = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        // Default: any assignee id resolves to a real account so EnsureAssigneeExists passes.
        _users.Setup(u => u.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => new UserSummaryDto { Id = id, DisplayName = id });
        _users.Setup(u => u.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserSummaryDto>());

        _sut = new TaskService(_repository.Object, _users.Object, NullLogger<TaskService>.Instance);
    }

    // ---------------------------------------------------------------- GetTasks (filtering / scope)

    [Fact]
    public async Task GetTasksAsync_ShouldReturnTasksAssignedToCaller_WhenNotAdmin()
    {
        _repository.Setup(r => r.GetForAssigneeAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                TaskItemBuilder.ForUser(UserId).WithStatus(TaskItemStatus.Todo).Build(),
                TaskItemBuilder.ForUser(UserId).WithStatus(TaskItemStatus.Done).Completed().Build()
            });

        var result = await _sut.GetTasksAsync(User, new TaskQueryParameters());

        result.Should().HaveCount(2);
        _repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTasksAsync_ShouldReturnAllTasks_WhenAdmin()
    {
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                TaskItemBuilder.ForUser(UserId).Build(),
                TaskItemBuilder.ForUser(OtherId).Build()
            });

        var result = await _sut.GetTasksAsync(Admin, new TaskQueryParameters());

        result.Should().HaveCount(2);
        _repository.Verify(r => r.GetForAssigneeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTasksAsync_ShouldFilterByAssignee_WhenAdminSuppliesAssigneeId()
    {
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                TaskItemBuilder.ForUser(UserId).Build(),
                TaskItemBuilder.ForUser(OtherId).Build()
            });

        var result = await _sut.GetTasksAsync(Admin, new TaskQueryParameters { AssigneeId = OtherId });

        result.Should().ContainSingle().Which.AssigneeId.Should().Be(OtherId);
    }

    [Fact]
    public async Task GetTasksAsync_ShouldFilterByStatus()
    {
        _repository.Setup(r => r.GetForAssigneeAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                TaskItemBuilder.ForUser(UserId).WithStatus(TaskItemStatus.Todo).Build(),
                TaskItemBuilder.ForUser(UserId).WithStatus(TaskItemStatus.InProgress).Build()
            });

        var result = await _sut.GetTasksAsync(User, new TaskQueryParameters { Status = TaskItemStatus.InProgress });

        result.Should().ContainSingle().Which.Status.Should().Be(TaskItemStatus.InProgress);
    }

    [Fact]
    public async Task GetTasksAsync_ShouldFilterBySearchTerm_CaseInsensitively()
    {
        _repository.Setup(r => r.GetForAssigneeAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                TaskItemBuilder.ForUser(UserId).WithTitle("Buy Milk").Build(),
                TaskItemBuilder.ForUser(UserId).WithTitle("Write report").Build()
            });

        var result = await _sut.GetTasksAsync(User, new TaskQueryParameters { Search = "milk" });

        result.Should().ContainSingle().Which.Title.Should().Be("Buy Milk");
    }

    [Fact]
    public async Task GetTasksAsync_ShouldResolveAssigneeName()
    {
        var task = TaskItemBuilder.ForUser(UserId).Build();
        _repository.Setup(r => r.GetForAssigneeAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { task });
        _users.Setup(u => u.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new UserSummaryDto { Id = UserId, DisplayName = "test1" } });

        var result = await _sut.GetTasksAsync(User, new TaskQueryParameters());

        result.Single().AssigneeName.Should().Be("test1");
    }

    // ---------------------------------------------------------------- GetById (access control)

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenTaskMissing()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var act = () => _sut.GetByIdAsync(User, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenTaskAssignedToAnotherUser()
    {
        var othersTask = TaskItemBuilder.ForUser(OtherId).Build();
        _repository.Setup(r => r.GetByIdAsync(othersTask.Id, It.IsAny<CancellationToken>())).ReturnsAsync(othersTask);

        var act = () => _sut.GetByIdAsync(User, othersTask.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTask_WhenAdminEvenIfAssignedElsewhere()
    {
        var othersTask = TaskItemBuilder.ForUser(OtherId).Build();
        _repository.Setup(r => r.GetByIdAsync(othersTask.Id, It.IsAny<CancellationToken>())).ReturnsAsync(othersTask);

        var result = await _sut.GetByIdAsync(Admin, othersTask.Id);

        result.Id.Should().Be(othersTask.Id);
    }

    // ---------------------------------------------------------------- Create

    [Fact]
    public async Task CreateAsync_ShouldAssignToCaller_AndAppendSortOrder()
    {
        _repository.Setup(r => r.GetMaxSortOrderAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(4);
        TaskItem? saved = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => saved = t)
            .Returns(Task.CompletedTask);

        var dto = new CreateTaskDto { Title = "New", Priority = TaskPriority.High };

        var result = await _sut.CreateAsync(User, dto);

        saved.Should().NotBeNull();
        saved!.UserId.Should().Be(UserId);
        saved.AssigneeId.Should().Be(UserId);
        saved.SortOrder.Should().Be(5); // max + 1
        result.Title.Should().Be("New");
        result.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task CreateAsync_ShouldIgnoreSuppliedAssignee_WhenCallerNotAdmin()
    {
        _repository.Setup(r => r.GetMaxSortOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        TaskItem? saved = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => saved = t)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(User, new CreateTaskDto { Title = "x", AssigneeId = OtherId });

        saved!.AssigneeId.Should().Be(UserId); // forced to self
    }

    [Fact]
    public async Task CreateAsync_ShouldHonourSuppliedAssignee_WhenAdmin()
    {
        _repository.Setup(r => r.GetMaxSortOrderAsync(OtherId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        TaskItem? saved = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => saved = t)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(Admin, new CreateTaskDto { Title = "x", AssigneeId = OtherId });

        saved!.AssigneeId.Should().Be(OtherId);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowValidation_WhenAdminAssignsToUnknownUser()
    {
        _users.Setup(u => u.FindByIdAsync("ghost", It.IsAny<CancellationToken>())).ReturnsAsync((UserSummaryDto?)null);

        var act = () => _sut.CreateAsync(Admin, new CreateTaskDto { Title = "x", AssigneeId = "ghost" });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldMarkCompleted_WhenStatusIsDone()
    {
        _repository.Setup(r => r.GetMaxSortOrderAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repository.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(User, new CreateTaskDto { Title = "Done one", Status = TaskItemStatus.Done });

        result.IsCompleted.Should().BeTrue();
    }

    // ---------------------------------------------------------------- Update (partial)

    [Fact]
    public async Task UpdateAsync_ShouldOnlyChangeSuppliedFields_AndSetUpdatedAt()
    {
        var existing = TaskItemBuilder.ForUser(UserId).WithTitle("Original").WithPriority(TaskPriority.Low).Build();
        _repository.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(User, existing.Id, new UpdateTaskDto { Title = "Renamed" });

        result.Title.Should().Be("Renamed");
        result.Priority.Should().Be(TaskPriority.Low); // unchanged
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFound_WhenTaskAssignedToAnotherUser()
    {
        var othersTask = TaskItemBuilder.ForUser(OtherId).Build();
        _repository.Setup(r => r.GetByIdAsync(othersTask.Id, It.IsAny<CancellationToken>())).ReturnsAsync(othersTask);

        var act = () => _sut.UpdateAsync(User, othersTask.Id, new UpdateTaskDto { Title = "x" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------------------------------------------------------------- SetCompletion

    [Fact]
    public async Task SetCompletionAsync_ShouldMoveTaskToDone_WhenCompleted()
    {
        var task = TaskItemBuilder.ForUser(UserId).WithStatus(TaskItemStatus.InProgress).Build();
        _repository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _repository.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.SetCompletionAsync(User, task.Id, true);

        result.IsCompleted.Should().BeTrue();
        result.Status.Should().Be(TaskItemStatus.Done);
    }

    [Fact]
    public async Task SetCompletionAsync_ShouldReopenToTodo_WhenUncompletingDoneTask()
    {
        var task = TaskItemBuilder.ForUser(UserId).WithStatus(TaskItemStatus.Done).Completed().Build();
        _repository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _repository.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.SetCompletionAsync(User, task.Id, false);

        result.IsCompleted.Should().BeFalse();
        result.Status.Should().Be(TaskItemStatus.Todo);
    }

    // ---------------------------------------------------------------- Assign (admin)

    [Fact]
    public async Task AssignAsync_ShouldReassignTask_AndAppendToNewAssigneeList_WhenAdmin()
    {
        var task = TaskItemBuilder.ForUser(UserId).Build();
        _repository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _repository.Setup(r => r.GetMaxSortOrderAsync(OtherId, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _repository.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.AssignAsync(Admin, task.Id, OtherId);

        result.AssigneeId.Should().Be(OtherId);
        task.SortOrder.Should().Be(3); // appended to end of new assignee's list
        _repository.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignAsync_ShouldThrowNotFound_WhenCallerNotAdmin()
    {
        var task = TaskItemBuilder.ForUser(UserId).Build();

        var act = () => _sut.AssignAsync(User, task.Id, OtherId);

        await act.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignAsync_ShouldThrowValidation_WhenAssigneeUnknown()
    {
        var task = TaskItemBuilder.ForUser(UserId).Build();
        _repository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _users.Setup(u => u.FindByIdAsync("ghost", It.IsAny<CancellationToken>())).ReturnsAsync((UserSummaryDto?)null);

        var act = () => _sut.AssignAsync(Admin, task.Id, "ghost");

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---------------------------------------------------------------- Reorder

    [Fact]
    public async Task ReorderAsync_ShouldAssignSortOrderByPosition_AndIgnoreUnknownIds()
    {
        var a = TaskItemBuilder.ForUser(UserId).WithSortOrder(0).Build();
        var b = TaskItemBuilder.ForUser(UserId).WithSortOrder(1).Build();
        _repository.Setup(r => r.GetForAssigneeAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { a, b });
        IEnumerable<TaskItem>? updated = null;
        _repository.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<TaskItem>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TaskItem>, CancellationToken>((t, _) => updated = t.ToList())
            .Returns(Task.CompletedTask);

        await _sut.ReorderAsync(User, new ReorderTasksDto { OrderedTaskIds = new[] { b.Id, a.Id, Guid.NewGuid() } });

        b.SortOrder.Should().Be(0);
        a.SortOrder.Should().Be(1);
        updated.Should().NotBeNull();
        updated!.Should().HaveCount(2);
    }

    // ---------------------------------------------------------------- Delete

    [Fact]
    public async Task DeleteAsync_ShouldThrowNotFound_WhenTaskMissing()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var act = () => _sut.DeleteAsync(User, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.DeleteAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTask_WhenAssignedToCaller()
    {
        var task = TaskItemBuilder.ForUser(UserId).Build();
        _repository.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _repository.Setup(r => r.DeleteAsync(task, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(User, task.Id);

        _repository.Verify(r => r.DeleteAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }
}

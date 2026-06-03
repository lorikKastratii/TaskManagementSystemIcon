using FluentAssertions;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Application.Tasks.Mapping;
using TaskManager.Domain.Enums;
using TaskManager.UnitTests.Builders;

namespace TaskManager.UnitTests.Tasks;

public class TaskMappingsTests
{
    [Fact]
    public void ToDto_ShouldCopyAllFields()
    {
        var task = TaskItemBuilder.ForUser("user-1")
            .WithTitle("Map me")
            .WithStatus(TaskItemStatus.InProgress)
            .WithPriority(TaskPriority.High)
            .Build();

        var dto = task.ToDto();

        dto.Id.Should().Be(task.Id);
        dto.Title.Should().Be("Map me");
        dto.Status.Should().Be(TaskItemStatus.InProgress);
        dto.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public void ApplyUpdate_ShouldTrimTitle_AndKeepCompletionConsistentWithStatus()
    {
        var task = TaskItemBuilder.ForUser("user-1").Build();

        task.ApplyUpdate(new UpdateTaskDto { Title = "  Trimmed  ", Status = TaskItemStatus.Done });

        task.Title.Should().Be("Trimmed");
        task.Status.Should().Be(TaskItemStatus.Done);
        task.IsCompleted.Should().BeTrue();
    }
}

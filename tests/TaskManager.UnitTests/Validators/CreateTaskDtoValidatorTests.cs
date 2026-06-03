using FluentAssertions;
using FluentValidation.TestHelper;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Application.Tasks.Validators;
using TaskManager.Domain.Enums;

namespace TaskManager.UnitTests.Validators;

public class CreateTaskDtoValidatorTests
{
    private readonly CreateTaskDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_ForValidDto()
    {
        var dto = new CreateTaskDto { Title = "Valid", Priority = TaskPriority.High };

        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Should_Fail_WhenTitleEmpty(string? title)
    {
        var dto = new CreateTaskDto { Title = title! };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_Fail_WhenTitleTooLong()
    {
        var dto = new CreateTaskDto { Title = new string('x', 201) };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_Fail_WhenDueDateInPast()
    {
        var dto = new CreateTaskDto { Title = "Has past due", DueDate = DateTime.UtcNow.AddDays(-1) };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void Should_Pass_WhenDueDateInFuture()
    {
        var dto = new CreateTaskDto { Title = "Future due", DueDate = DateTime.UtcNow.AddDays(1) };

        _validator.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }
}

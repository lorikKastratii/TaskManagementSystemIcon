using FluentValidation.TestHelper;
using TaskManager.Application.Ai.Dtos;
using TaskManager.Application.Ai.Validators;

namespace TaskManager.UnitTests.Validators;

public class EnhanceDescriptionRequestDtoValidatorTests
{
    private readonly EnhanceDescriptionRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_ForValidRequest()
    {
        var dto = new EnhanceDescriptionRequestDto { Title = "Add login", Description = "users can log in" };

        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_WhenDescriptionEmpty()
    {
        var dto = new EnhanceDescriptionRequestDto { Title = "Add login", Description = null };

        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Should_Fail_WhenTitleEmpty(string? title)
    {
        var dto = new EnhanceDescriptionRequestDto { Title = title! };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_Fail_WhenDescriptionTooLong()
    {
        var dto = new EnhanceDescriptionRequestDto { Title = "Valid", Description = new string('x', 1001) };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Description);
    }
}

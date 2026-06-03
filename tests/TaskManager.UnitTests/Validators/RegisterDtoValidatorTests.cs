using FluentValidation.TestHelper;
using TaskManager.Application.Auth.Dtos;
using TaskManager.Application.Auth.Validators;

namespace TaskManager.UnitTests.Validators;

public class RegisterDtoValidatorTests
{
    private readonly RegisterDtoValidator _validator = new();

    [Fact]
    public void Should_Pass_ForValidRegistration()
    {
        var dto = new RegisterDto { Email = "user@example.com", Password = "Passw0rd", ConfirmPassword = "Passw0rd" };

        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_ForInvalidEmail()
    {
        var dto = new RegisterDto { Email = "not-an-email", Password = "Passw0rd", ConfirmPassword = "Passw0rd" };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_WhenPasswordTooWeak()
    {
        // Missing uppercase and digit.
        var dto = new RegisterDto { Email = "user@example.com", Password = "password", ConfirmPassword = "password" };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Fail_WhenPasswordsDoNotMatch()
    {
        var dto = new RegisterDto { Email = "user@example.com", Password = "Passw0rd", ConfirmPassword = "Other1rd" };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }
}

using FluentValidation;
using TaskManager.Application.Auth.Dtos;

namespace TaskManager.Application.Auth.Validators;

/// <summary>Validation rules for login.</summary>
public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

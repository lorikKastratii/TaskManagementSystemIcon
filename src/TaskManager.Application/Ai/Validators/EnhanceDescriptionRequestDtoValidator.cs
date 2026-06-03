using FluentValidation;
using TaskManager.Application.Ai.Dtos;

namespace TaskManager.Application.Ai.Validators;

/// <summary>Validation rules for the AI enhancement request.</summary>
public class EnhanceDescriptionRequestDtoValidator : AbstractValidator<EnhanceDescriptionRequestDto>
{
    public EnhanceDescriptionRequestDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Add a title first so the AI has something to work with.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
    }
}

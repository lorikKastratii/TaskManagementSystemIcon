using FluentValidation;
using TaskManager.Application.Tasks.Dtos;

namespace TaskManager.Application.Tasks.Validators;

/// <summary>Validation rules for creating a task.</summary>
public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
{
    public CreateTaskDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status must be a valid value (Todo, InProgress, Done).");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Priority must be a valid value (Low, Medium, High).");

        RuleFor(x => x.DueDate)
            .Must(date => date is null || date.Value.Date >= DateTime.UtcNow.Date)
            .WithMessage("Due date cannot be in the past.");
    }
}

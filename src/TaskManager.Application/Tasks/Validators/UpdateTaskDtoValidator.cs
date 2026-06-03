using FluentValidation;
using TaskManager.Application.Tasks.Dtos;

namespace TaskManager.Application.Tasks.Validators;

/// <summary>
/// Validation rules for updating a task. All fields are optional (partial update),
/// so each rule only runs when the corresponding value is supplied.
/// </summary>
public class UpdateTaskDtoValidator : AbstractValidator<UpdateTaskDto>
{
    public UpdateTaskDtoValidator()
    {
        When(x => x.Title is not null, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title cannot be empty.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
        });

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
        });

        When(x => x.Status.HasValue, () =>
        {
            RuleFor(x => x.Status!.Value)
                .IsInEnum().WithMessage("Status must be a valid value (Todo, InProgress, InReview, Done).");
        });

        When(x => x.Priority.HasValue, () =>
        {
            RuleFor(x => x.Priority!.Value)
                .IsInEnum().WithMessage("Priority must be a valid value (Low, Medium, High).");
        });

        When(x => x.DueDate.HasValue, () =>
        {
            RuleFor(x => x.DueDate!.Value)
                .Must(date => date.Date >= DateTime.UtcNow.Date)
                .WithMessage("Due date cannot be in the past.");
        });
    }
}

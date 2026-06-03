namespace TaskManager.Application.Ai.Dtos;

/// <summary>
/// Payload for the "Enhance with AI" action. The title gives the model context; the (optional)
/// current description is the draft it should improve. Validated by
/// <see cref="Validators.EnhanceDescriptionRequestDtoValidator"/>.
/// </summary>
public record EnhanceDescriptionRequestDto
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
}

namespace TaskManager.Application.Ai.Dtos;

/// <summary>The AI-improved task description returned to the client.</summary>
public record EnhancedDescriptionDto
{
    public string Description { get; init; } = string.Empty;
}

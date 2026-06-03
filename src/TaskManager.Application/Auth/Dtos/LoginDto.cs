namespace TaskManager.Application.Auth.Dtos;

/// <summary>Login request. Validated by LoginDtoValidator.</summary>
public record LoginDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

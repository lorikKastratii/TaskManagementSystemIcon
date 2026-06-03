namespace TaskManager.Application.Auth.Dtos;

/// <summary>Registration request. Validated by RegisterDtoValidator.</summary>
public record RegisterDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
}

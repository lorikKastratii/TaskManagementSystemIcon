namespace TaskManager.Application.Auth.Dtos;

/// <summary>Returned on successful register/login. Carries the JWT and basic identity info.</summary>
public record AuthResponseDto
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

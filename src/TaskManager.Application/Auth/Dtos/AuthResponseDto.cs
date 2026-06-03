namespace TaskManager.Application.Auth.Dtos;

/// <summary>Returned on successful register/login. Carries the JWT and basic identity info.</summary>
public record AuthResponseDto
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    /// <summary>Roles held by the user (e.g. "Admin"), so the client can adapt its UI.</summary>
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

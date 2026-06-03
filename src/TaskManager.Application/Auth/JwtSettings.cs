namespace TaskManager.Application.Auth;

/// <summary>
/// Strongly-typed JWT configuration bound from the "Jwt" configuration section.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Symmetric signing key. MUST be supplied via configuration/secret, never hard-coded.</summary>
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "TaskManager.API";

    public string Audience { get; set; } = "TaskManager.Client";

    /// <summary>Token lifetime in hours.</summary>
    public int ExpiryHours { get; set; } = 12;
}

namespace TaskManager.Application.Ai;

/// <summary>
/// Strongly-typed configuration bound from the "OpenAI" configuration section. Drives the
/// <see cref="Interfaces.IAiTaskAssistant"/> implementation. The API key MUST be supplied via
/// configuration/secret (env var <c>OpenAI__ApiKey</c> or user-secrets), never hard-coded.
/// </summary>
public class OpenAiSettings
{
    public const string SectionName = "OpenAI";

    /// <summary>OpenAI API key. When empty, AI enhancement is treated as not configured.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Chat model used for enhancement. Defaults to a fast, low-cost model.</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>API base URL — override to target Azure OpenAI or a compatible proxy.</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>Upper bound on generated tokens, keeping responses (and cost) in check.</summary>
    public int MaxOutputTokens { get; set; } = 500;
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManager.Application.Ai;
using TaskManager.Application.Ai.Dtos;
using TaskManager.Application.Ai.Interfaces;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Infrastructure.Ai;

/// <summary>
/// <see cref="IAiTaskAssistant"/> backed by the OpenAI Chat Completions API. Lives in the
/// Infrastructure layer because it talks to an external service; the Application layer depends
/// only on the interface. Failures are surfaced as <see cref="ValidationException"/> so the
/// existing ProblemDetails middleware returns a clean, client-friendly 400.
/// </summary>
public class OpenAiTaskAssistant : IAiTaskAssistant
{
    // The model is told to act as a Product Owner and to return ONLY the rewritten description.
    private const string SystemPrompt =
        "You are an experienced Product Owner on a software team. Rewrite the user's task description " +
        "so it is clear, concise and actionable for developers. Preserve the original intent and any " +
        "concrete details. Where it helps, add a short 'Acceptance criteria:' section with 2-4 bullet " +
        "points. Do not invent requirements that contradict the input. Respond with ONLY the improved " +
        "description as plain text (no markdown headings, no preamble, no quotes). Keep it under 800 characters.";

    private readonly HttpClient _http;
    private readonly OpenAiSettings _settings;
    private readonly ILogger<OpenAiTaskAssistant> _logger;

    public OpenAiTaskAssistant(HttpClient http, IOptions<OpenAiSettings> settings, ILogger<OpenAiTaskAssistant> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _http = http;
        _http.BaseAddress ??= new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<EnhancedDescriptionDto> EnhanceDescriptionAsync(EnhanceDescriptionRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new ValidationException("ai", "AI enhancement is not configured on the server.");
        }

        var payload = BuildRequestPayload(request);
        var response = await SendAsync(payload, ct);
        var text = await ReadCompletionTextAsync(response, ct);

        _logger.LogInformation("Enhanced a task description via OpenAI ({Model}).", _settings.Model);
        return new EnhancedDescriptionDto { Description = text };
    }

    private ChatRequest BuildRequestPayload(EnhanceDescriptionRequestDto request)
    {
        var draft = string.IsNullOrWhiteSpace(request.Description) ? "(no description yet)" : request.Description!.Trim();
        var userPrompt = $"Task title: {request.Title.Trim()}\n\nCurrent description:\n{draft}";

        return new ChatRequest(
            Model: _settings.Model,
            Messages: new[]
            {
                new ChatMessage("system", SystemPrompt),
                new ChatMessage("user", userPrompt),
            },
            Temperature: 0.7,
            MaxTokens: _settings.MaxOutputTokens);
    }

    private async Task<HttpResponseMessage> SendAsync(ChatRequest payload, CancellationToken ct)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(payload),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        try
        {
            return await _http.SendAsync(httpRequest, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Failed to reach OpenAI for description enhancement.");
            throw new ValidationException("ai", "Could not reach the AI service. Please try again.");
        }
    }

    private async Task<string> ReadCompletionTextAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("OpenAI returned {Status}: {Body}", (int)response.StatusCode, body);
            throw new ValidationException("ai", "The AI service rejected the request. Please try again later.");
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatResponse>(ct);
        var text = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("OpenAI returned an empty completion for an enhancement request.");
            throw new ValidationException("ai", "The AI service returned an empty response. Please try again.");
        }

        return text;
    }

    // Minimal OpenAI Chat Completions request/response shapes (only the fields we use).

    private record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record ChatResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatChoice>? Choices);

    private record ChatChoice(
        [property: JsonPropertyName("message")] ChatMessage? Message);
}

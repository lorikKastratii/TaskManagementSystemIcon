using TaskManager.Application.Ai.Dtos;

namespace TaskManager.Application.Ai.Interfaces;

/// <summary>
/// AI assistance for authoring tasks. Implemented in the Infrastructure layer over an external
/// LLM provider, so the Application layer stays provider-agnostic and unit-testable.
/// </summary>
public interface IAiTaskAssistant
{
    /// <summary>
    /// Rewrites a task description as a Product Owner would: clearer, well-structured and
    /// actionable. Throws <see cref="Common.Exceptions.ValidationException"/> when the feature is
    /// not configured or the provider rejects the request.
    /// </summary>
    Task<EnhancedDescriptionDto> EnhanceDescriptionAsync(EnhanceDescriptionRequestDto request, CancellationToken ct = default);
}

namespace TaskManager.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist (or is not visible to the current user).
/// Translated to an HTTP 404 by the global exception-handling middleware.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string resource, object key)
        : base($"{resource} with id '{key}' was not found.") { }
}

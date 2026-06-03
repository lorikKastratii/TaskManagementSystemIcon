namespace TaskManager.Application.Common.Exceptions;

/// <summary>
/// Thrown when request data fails business/validation rules outside the automatic
/// model-validation pipeline (e.g. authentication failures surfaced as field errors).
/// Translated to an HTTP 400 with a field-keyed error dictionary by the middleware.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = new[] { error } }) { }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

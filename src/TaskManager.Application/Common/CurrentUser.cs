namespace TaskManager.Application.Common;

/// <summary>
/// The authenticated caller as seen by the Application layer. Carries just enough identity
/// (the user id and whether they hold the Admin role) for services to make access decisions
/// without depending on ASP.NET types. Built by the API layer from the validated JWT.
/// </summary>
/// <param name="Id">The caller's Identity user id (from the JWT NameIdentifier claim).</param>
/// <param name="IsAdmin">True when the caller is in the Admin role and may cross user boundaries.</param>
public readonly record struct CurrentUser(string Id, bool IsAdmin);

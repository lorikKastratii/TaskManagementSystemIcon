namespace TaskManager.Application.Auth.Interfaces;

/// <summary>Generates signed JWT access tokens. Implemented in the Application layer.</summary>
public interface ITokenService
{
    /// <summary>
    /// Builds a signed JWT for the given user, embedding their roles as role claims so the API
    /// can authorise by role and the client can adapt its UI.
    /// </summary>
    /// <returns>The encoded token and its UTC expiry.</returns>
    (string Token, DateTime ExpiresAt) CreateToken(string userId, string email, IEnumerable<string> roles);
}

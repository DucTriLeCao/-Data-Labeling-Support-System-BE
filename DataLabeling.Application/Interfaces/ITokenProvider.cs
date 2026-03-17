namespace DataLabeling.Application.Interfaces;

public interface ITokenProvider
{
    /// <summary>
    /// Generate JWT access token
    /// </summary>
    string GenerateAccessToken(long userId, string username, string role);

    /// <summary>
    /// Generate refresh token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validate and get claims from JWT token
    /// </summary>
    System.Security.Claims.ClaimsPrincipal? ValidateToken(string token);
}

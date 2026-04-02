using DataLabeling.Domain.DTOs.Auth;

namespace DataLabeling.Application.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Login user and return tokens
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Validate user credentials
    /// </summary>
    Task<bool> ValidateCredentialsAsync(string username, string password);

    /// <summary>
    /// Get user by username
    /// </summary>
    Task<UserDto?> GetUserByUsernameAsync(string username);


}

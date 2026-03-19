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

    /// <summary>
    /// Request password reset
    /// </summary>
    Task<AuthResponse> ForgotPasswordAsync(string emailOrUsername);

    /// <summary>
    /// Reset password with token
    /// </summary>
    Task<AuthResponse> ResetPasswordAsync(long userId, string resetToken, string newPassword, string confirmPassword);

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    Task<AuthResponse> ChangePasswordAsync(long userId, ChangePasswordRequest request);
}

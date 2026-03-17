namespace DataLabeling.Application.Interfaces;

public interface IPasswordResetService
{
    /// <summary>
    /// Generate and send password reset token
    /// </summary>
    Task<(bool Success, string Token, string Message)> GeneratePasswordResetTokenAsync(string emailOrUsername);

    /// <summary>
    /// Validate password reset token
    /// </summary>
    Task<(bool IsValid, long UserId)> ValidateResetTokenAsync(long userId, string token);

    /// <summary>
    /// Reset password with token
    /// </summary>
    Task<(bool Success, string Message)> ResetPasswordAsync(long userId, string resetToken, string newPassword);

    /// <summary>
    /// Change password for logged-in user
    /// </summary>
    Task<(bool Success, string Message)> ChangePasswordAsync(long userId, string currentPassword, string newPassword);
}

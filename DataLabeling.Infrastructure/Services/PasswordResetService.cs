using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataLabeling.Infrastructure.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly DataLabelingDBContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly int _resetTokenExpirationMinutes = 30;

    public PasswordResetService(
        DataLabelingDBContext dbContext,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<(bool Success, string Token, string Message)> GeneratePasswordResetTokenAsync(string emailOrUsername)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(emailOrUsername))
            {
                return (false, string.Empty, "Email or username is required");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
                u.Email == emailOrUsername || u.Username == emailOrUsername);

            if (user == null)
            {
                // Don't reveal if user exists (security best practice)
                return (true, string.Empty, "If an account exists, a password reset email will be sent");
            }

            // Generate a secure random token
            var resetToken = GenerateSecureToken();
            var hashedToken = _passwordHasher.HashPassword(resetToken);

            // Invalidate previous tokens
            var previousTokens = _dbContext.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.IsUsed)
                .ToList();

            foreach (var token in previousTokens)
            {
                token.IsUsed = true;
            }

            // Create new reset token record
            var resetTokenRecord = new PasswordResetToken
            {
                UserId = user.Id,
                Token = hashedToken,
                ExpiryTime = DateTime.UtcNow.AddMinutes(_resetTokenExpirationMinutes),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.PasswordResetTokens.Add(resetTokenRecord);
            await _dbContext.SaveChangesAsync();

            // In a production environment, this would send an email with the token
            // For now, we'll return the token for testing purposes
            // Email template: user.Email + reset link with token

            return (true, resetToken, "Password reset token generated successfully");
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"An error occurred: {ex.Message}");
        }
    }

    public async Task<(bool IsValid, long UserId)> ValidateResetTokenAsync(long userId, string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, 0);
            }

            var resetTokenRecord = await _dbContext.PasswordResetTokens
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId &&
                    !t.IsUsed &&
                    t.ExpiryTime > DateTime.UtcNow);

            if (resetTokenRecord == null)
            {
                return (false, 0);
            }

            // Verify the token hash
            if (!_passwordHasher.VerifyPassword(token, resetTokenRecord.Token))
            {
                return (false, 0);
            }

            return (true, userId);
        }
        catch
        {
            return (false, 0);
        }
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(long userId, string resetToken, string newPassword)
    {
        try
        {
            // Validate password
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                return (false, "New password is required");
            }

            if (newPassword.Length < 6)
            {
                return (false, "Password must be at least 6 characters long");
            }

            // Validate token
            var (isValid, validatedUserId) = await ValidateResetTokenAsync(userId, resetToken);
            if (!isValid || validatedUserId != userId)
            {
                return (false, "Invalid or expired reset token");
            }

            // Get user
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            // Update password
            user.PasswordHash = _passwordHasher.HashPassword(newPassword);

            // Mark token as used
            var resetTokenRecord = await _dbContext.PasswordResetTokens
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId &&
                    !t.IsUsed &&
                    t.ExpiryTime > DateTime.UtcNow);

            if (resetTokenRecord != null)
            {
                resetTokenRecord.IsUsed = true;
            }

            await _dbContext.SaveChangesAsync();

            return (true, "Password reset successfully");
        }
        catch (Exception ex)
        {
            return (false, $"An error occurred: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(long userId, string currentPassword, string newPassword)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return (false, "Current password and new password are required");
            }

            if (newPassword.Length < 6)
            {
                return (false, "New password must be at least 6 characters long");
            }

            if (currentPassword == newPassword)
            {
                return (false, "New password must be different from current password");
            }

            // Get user
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            // Verify current password
            if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
            {
                return (false, "Current password is incorrect");
            }

            // Update password
            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            await _dbContext.SaveChangesAsync();

            return (true, "Password changed successfully");
        }
        catch (Exception ex)
        {
            return (false, $"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate a secure random token (URL-safe Base64)
    /// </summary>
    private string GenerateSecureToken()
    {
        var randomNumber = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}

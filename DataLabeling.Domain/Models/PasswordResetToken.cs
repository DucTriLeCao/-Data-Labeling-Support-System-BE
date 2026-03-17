namespace DataLabeling.Domain.Models;

/// <summary>
/// Model for storing password reset tokens
/// </summary>
public class PasswordResetToken
{
    public long Id { get; set; }

    public long UserId { get; set; }

    /// <summary>
    /// The reset token (hashed or encrypted)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime ExpiryTime { get; set; }

    /// <summary>
    /// Whether the token has been used
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// When the token was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User? User { get; set; }
}

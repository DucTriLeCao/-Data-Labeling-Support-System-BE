namespace DataLabeling.Domain.DTOs.Auth;

/// <summary>
/// Login request with email
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Email address for login
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;
}

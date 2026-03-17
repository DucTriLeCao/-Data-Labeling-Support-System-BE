namespace DataLabeling.Domain.DTOs.Auth;

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    /// <summary>
    /// User role: Admin, Manager, Annotator, or Reviewer (case-insensitive, defaults to Annotator)
    /// </summary>
    public string Role { get; set; } = "Annotator";
}

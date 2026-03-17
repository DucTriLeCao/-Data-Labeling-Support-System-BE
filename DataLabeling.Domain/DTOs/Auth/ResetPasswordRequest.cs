namespace DataLabeling.Domain.DTOs.Auth;

public class ResetPasswordRequest
{
    public long UserId { get; set; }
    public string ResetToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

namespace DataLabeling.Domain.DTOs.Auth;

public class ForgotPasswordRequest
{
    public string EmailOrUsername { get; set; } = string.Empty;
}

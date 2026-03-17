namespace DataLabeling.Domain.DTOs.Admin.UserMgmt;

public class UserDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

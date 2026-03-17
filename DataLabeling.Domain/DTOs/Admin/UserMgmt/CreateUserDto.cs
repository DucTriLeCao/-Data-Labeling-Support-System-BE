using System.ComponentModel.DataAnnotations;

namespace DataLabeling.Domain.DTOs.Admin.UserMgmt;

public class CreateUserDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}

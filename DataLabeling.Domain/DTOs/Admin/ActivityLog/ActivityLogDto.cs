namespace DataLabeling.Domain.DTOs.Admin.ActivityLog;

public class ActivityLogDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    public DateTime? CreatedAt { get; set; }
}

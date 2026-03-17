namespace DataLabeling.Domain.DTOs.Admin.ActivityLog;

public class ActivityLogQueryDto
{
    public long? UserId { get; set; }
    public string? Action { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

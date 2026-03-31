namespace DataLabeling.Domain.DTOs.Manager;

public class DatasetAssignmentDto
{
    public long Id { get; set; }
    public long DatasetId { get; set; }
    public long UserId { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
    public DateTime? AssignedAt { get; set; }
}

namespace DataLabeling.Domain.DTOs.Annotator;

/// <summary>
/// DTO for assigned annotation task
/// </summary>
public class AssignedTaskDto
{
    public long DataItemAssignmentId { get; set; }
    public long DataItemId { get; set; }
    public long DatasetId { get; set; }
    public string? DatasetName { get; set; }
    public string? DataContent { get; set; }
    public string? Status { get; set; }
    public DateTime? AssignedAt { get; set; }
    public bool HasAnnotation { get; set; }
    public string? CurrentAnnotationStatus { get; set; }
}

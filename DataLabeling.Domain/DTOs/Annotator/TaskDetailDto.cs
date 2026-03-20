namespace DataLabeling.Domain.DTOs.Annotator;

/// <summary>
/// DTO for task details including instructions and labels
/// </summary>
public class TaskDetailDto
{
    public long DataItemAssignmentId { get; set; }
    public long DataItemId { get; set; }
    public long DatasetId { get; set; }
    public long ProjectId { get; set; }
    public string? DatasetName { get; set; }
    public string? ProjectName { get; set; }
    public string? DataContent { get; set; }
    public string? ProjectDescription { get; set; } // Can be used as instructions
    public DateTime? AssignedAt { get; set; }
    public List<LabelOptionDto> AvailableLabels { get; set; } = new();
    public AnnotationDetailDto? CurrentAnnotation { get; set; }
}

namespace DataLabeling.Domain.DTOs.Annotator;

/// <summary>
/// DTO for feedback from reviewers
/// </summary>
public class AnnotationFeedbackDto
{
    public long ReviewId { get; set; }
    public long AnnotationId { get; set; }
    public string? ReviewStatus { get; set; }
    public string? Comment { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

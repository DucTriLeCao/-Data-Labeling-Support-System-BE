namespace DataLabeling.Domain.DTOs.Reviewer;

/// <summary>
/// One annotation item waiting for reviewer decision
/// </summary>
public class ReviewQueueItemDto
{
    public long AnnotationId { get; set; }
    public long DataItemId { get; set; }
    public long DatasetId { get; set; }
    public long ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? DatasetName { get; set; }
    public string? DataContent { get; set; }
    public string? LabelValue { get; set; }
    public string? AnnotationType { get; set; }
    public string? CoordinateData { get; set; }
    public string? AnnotationStatus { get; set; }
    public long AnnotatorId { get; set; }
    public string? AnnotatorName { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

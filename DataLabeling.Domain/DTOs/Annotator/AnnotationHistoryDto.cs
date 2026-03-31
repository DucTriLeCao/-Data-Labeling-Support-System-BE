namespace DataLabeling.Domain.DTOs.Annotator;

/// <summary>
/// History of annotation work by an annotator
/// </summary>
public class AnnotationHistoryDto
{
    public long AnnotationId { get; set; }
    public long DataItemId { get; set; }
    public long DatasetId { get; set; }
    public long ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? DatasetName { get; set; }
    public string? LabelValue { get; set; }
    public string? Status { get; set; }
    public string? AnnotationType { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int ReviewCount { get; set; }
    public string? LatestReviewDecision { get; set; }
}

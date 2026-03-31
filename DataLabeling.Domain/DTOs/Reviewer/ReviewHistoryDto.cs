namespace DataLabeling.Domain.DTOs.Reviewer;

/// <summary>
/// History of review work completed by a reviewer
/// </summary>
public class ReviewHistoryDto
{
    public long ReviewId { get; set; }
    public long AnnotationId { get; set; }
    public long DataItemId { get; set; }
    public long DatasetId { get; set; }
    public long ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? DatasetName { get; set; }
    public long AnnotatorId { get; set; }
    public string? AnnotatorName { get; set; }
    public string? LabelValue { get; set; }
    public string? ReviewStatus { get; set; }
    public string? Comment { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? AnnotationSubmittedAt { get; set; }
}

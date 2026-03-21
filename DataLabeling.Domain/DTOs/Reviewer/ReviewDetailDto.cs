namespace DataLabeling.Domain.DTOs.Reviewer;

/// <summary>
/// Detailed information used by reviewer to validate annotation against project guidance
/// </summary>
public class ReviewDetailDto
{
    public long AnnotationId { get; set; }
    public long DataItemId { get; set; }
    public long DatasetId { get; set; }
    public long ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? DatasetName { get; set; }
    public string? DataContent { get; set; }
    public string? ProjectInstruction { get; set; }
    public string? LabelValue { get; set; }
    public string? AnnotationType { get; set; }
    public string? CoordinateData { get; set; }
    public long AnnotatorId { get; set; }
    public string? AnnotatorName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public List<string> AllowedLabels { get; set; } = new();
    public ValidationResultDto Validation { get; set; } = new();
}

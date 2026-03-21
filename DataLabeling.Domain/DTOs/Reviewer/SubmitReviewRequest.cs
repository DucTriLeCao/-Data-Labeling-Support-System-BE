namespace DataLabeling.Domain.DTOs.Reviewer;

/// <summary>
/// Request for approving or returning an annotation for rework
/// </summary>
public class SubmitReviewRequest
{
    public long AnnotationId { get; set; }
    public string? Decision { get; set; } // Approved or NeedsRework
    public string? Comment { get; set; }
    public List<string> ErrorCategories { get; set; } = new();
}

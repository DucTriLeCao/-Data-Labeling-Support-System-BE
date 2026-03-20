namespace DataLabeling.Domain.DTOs.Annotator;

/// <summary>
/// DTO for annotation details with spatial data support
/// </summary>
public class AnnotationDetailDto
{
    public long AnnotationId { get; set; }
    public long DataItemId { get; set; }
    public string? LabelValue { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    
    /// <summary>
    /// Annotation type: "bbox", "polygon", "point", "segmentation"
    /// </summary>
    public string? AnnotationType { get; set; }
    
    /// <summary>
    /// JSON format coordinates for spatial annotations
    /// </summary>
    public string? CoordinateData { get; set; }
    
    public List<AnnotationFeedbackDto>? Feedbacks { get; set; }
}

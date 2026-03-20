namespace DataLabeling.Domain.DTOs.Annotator;

/// <summary>
/// DTO for creating/updating annotation with optional spatial data
/// </summary>
public class CreateAnnotationRequest
{
    public long DataItemAssignmentId { get; set; }
    public long DataItemId { get; set; }
    public string? LabelValue { get; set; } // The selected label (e.g., "cat", "dog")
    
    /// <summary>
    /// Annotation type: "bbox" (bounding box), "polygon", "point", "segmentation"
    /// Null if only text label without spatial annotation
    /// </summary>
    public string? AnnotationType { get; set; }
    
    /// <summary>
    /// JSON format coordinates
    /// Examples:
    /// - Bbox: {"x1": 100, "y1": 150, "x2": 400, "y2": 500}
    /// - Polygon: {"points": [[100,150], [400,150], [400,500], [100,500]]}
    /// - Point: {"x": 250, "y": 300}
    /// </summary>
    public string? CoordinateData { get; set; }
}

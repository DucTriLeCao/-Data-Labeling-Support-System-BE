namespace DataLabeling.Domain.DTOs.Annotator;

/// <summary>
/// DTO for annotator profile information
/// </summary>
public class AnnotatorProfileDto
{
    public long UserId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    
    /// <summary>
    /// Total number of annotations created by the annotator
    /// </summary>
    public int TotalAnnotations { get; set; }
    
    /// <summary>
    /// Number of approved annotations
    /// </summary>
    public int ApprovedAnnotations { get; set; }
    
    /// <summary>
    /// Number of pending annotations awaiting review
    /// </summary>
    public int PendingAnnotations { get; set; }
    
    /// <summary>
    /// Number of rejected annotations
    /// </summary>
    public int RejectedAnnotations { get; set; }
    
    /// <summary>
    /// Accuracy score calculated as (ApprovedAnnotations / TotalAnnotations) * 100
    /// </summary>
    public double AccuracyScore { get; set; }
    
    /// <summary>
    /// Date when the annotator joined
    /// </summary>
    public DateTime? JoinedDate { get; set; }
}

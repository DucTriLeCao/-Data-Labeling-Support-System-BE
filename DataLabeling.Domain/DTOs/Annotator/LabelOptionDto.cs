namespace DataLabeling.Domain.DTOs.Annotator;

/// <summary>
/// DTO for available label options
/// </summary>
public class LabelOptionDto
{
    public long LabelId { get; set; }
    public string? LabelName { get; set; }
    public long? ParentLabelId { get; set; }
    public string? ParentLabelName { get; set; }
}

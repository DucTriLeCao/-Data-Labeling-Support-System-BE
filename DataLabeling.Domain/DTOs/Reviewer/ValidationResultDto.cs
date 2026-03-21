namespace DataLabeling.Domain.DTOs.Reviewer;

/// <summary>
/// Validation output for reviewer checks before making a decision
/// </summary>
public class ValidationResultDto
{
    public bool IsValid { get; set; }
    public bool LabelExistsInProject { get; set; }
    public bool IsCoordinateJsonValid { get; set; }
    public List<string> Issues { get; set; } = new();
}

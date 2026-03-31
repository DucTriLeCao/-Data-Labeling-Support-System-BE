namespace DataLabeling.Domain.DTOs.Common;

/// <summary>
/// Result of bulk delete operation
/// </summary>
public class BulkDeleteResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessfullyDeleted { get; set; }
    public int Failed { get; set; }
    public List<BulkDeleteErrorDto> Errors { get; set; } = new();
}

public class BulkDeleteErrorDto
{
    public long Id { get; set; }
    public string? Reason { get; set; }
}

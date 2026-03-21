namespace DataLabeling.Domain.DTOs.Reviewer;

/// <summary>
/// DTO for reviewer operation responses
/// </summary>
public class ReviewerResponse<T>
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

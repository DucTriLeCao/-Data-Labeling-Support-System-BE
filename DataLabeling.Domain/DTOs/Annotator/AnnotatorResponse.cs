namespace DataLabeling.Domain.DTOs.Annotator;

/// <summary>
/// DTO for annotator operation responses
/// </summary>
public class AnnotatorResponse<T>
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

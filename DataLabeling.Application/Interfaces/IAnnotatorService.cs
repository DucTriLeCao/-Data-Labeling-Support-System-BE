using DataLabeling.Domain.DTOs.Annotator;

namespace DataLabeling.Application.Interfaces;

/// <summary>
/// Interface for annotator-related operations
/// </summary>
public interface IAnnotatorService
{
    /// <summary>
    /// Get all assigned tasks for an annotator
    /// </summary>
    Task<AnnotatorResponse<List<AssignedTaskDto>>> GetAssignedTasksAsync(long userId);

    /// <summary>
    /// Get detailed information about a task including instructions and available labels
    /// </summary>
    Task<AnnotatorResponse<TaskDetailDto>> GetTaskDetailAsync(long userId, long dataItemAssignmentId);

    /// <summary>
    /// Create or update annotation for a data item
    /// </summary>
    Task<AnnotatorResponse<AnnotationDetailDto>> CreateOrUpdateAnnotationAsync(long userId, CreateAnnotationRequest request);

    /// <summary>
    /// Submit annotation for review
    /// </summary>
    Task<AnnotatorResponse<string>> SubmitAnnotationForReviewAsync(long userId, SubmitAnnotationRequest request);

    /// <summary>
    /// Get feedback on an annotation
    /// </summary>
    Task<AnnotatorResponse<AnnotationDetailDto>> GetAnnotationWithFeedbackAsync(long userId, long annotationId);
}

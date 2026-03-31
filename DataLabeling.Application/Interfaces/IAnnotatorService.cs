using DataLabeling.Domain.DTOs.Annotator;
using DataLabeling.Domain.DTOs.Common;

namespace DataLabeling.Application.Interfaces;

/// <summary>
/// Interface for annotator-related operations
/// </summary>
public interface IAnnotatorService
{
    /// <summary>
    /// Get all assigned tasks for an annotator
    /// </summary>
    Task<AnnotatorResponse<PagedResult<AssignedTaskDto>>> GetAssignedTasksAsync(long userId, int pageNumber = 1, int pageSize = 20);

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

    /// <summary>
    /// Get work history of annotations created by the annotator
    /// </summary>
    Task<AnnotatorResponse<PagedResult<AnnotationHistoryDto>>> GetAnnotationHistoryAsync(long userId, int pageNumber = 1, int pageSize = 20, string? status = null);

    /// <summary>
    /// Get the current annotator's profile information including statistics
    /// </summary>
    Task<AnnotatorResponse<AnnotatorProfileDto>> GetAnnotatorProfileAsync(long userId);
}

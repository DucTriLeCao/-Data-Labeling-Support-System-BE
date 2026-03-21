using DataLabeling.Domain.DTOs.Reviewer;

namespace DataLabeling.Application.Interfaces;

/// <summary>
/// Interface for reviewer-related operations
/// </summary>
public interface IReviewerService
{
    /// <summary>
    /// Get submitted annotations that the current reviewer can review
    /// </summary>
    Task<ReviewerResponse<List<ReviewQueueItemDto>>> GetSubmittedQueueAsync(long reviewerId);

    /// <summary>
    /// Get detail and validation information for one submitted annotation
    /// </summary>
    Task<ReviewerResponse<ReviewDetailDto>> GetReviewDetailAsync(long reviewerId, long annotationId);

    /// <summary>
    /// Approve annotation or return for rework, with optional categorized errors
    /// </summary>
    Task<ReviewerResponse<string>> SubmitReviewDecisionAsync(long reviewerId, SubmitReviewRequest request);
}

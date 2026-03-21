using System.Security.Claims;
using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Reviewer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataLabeling.API.Controllers;

/// <summary>
/// API endpoints for reviewer operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Reviewer")]
public class ReviewerController : ControllerBase
{
    private readonly IReviewerService _reviewerService;
    private readonly ILogger<ReviewerController> _logger;

    public ReviewerController(IReviewerService reviewerService, ILogger<ReviewerController> logger)
    {
        _reviewerService = reviewerService ?? throw new ArgumentNullException(nameof(reviewerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private long GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get list of submitted annotations waiting for reviewer decision
    /// </summary>
    [HttpGet("submitted-queue")]
    [ProducesResponseType(typeof(ReviewerResponse<List<ReviewQueueItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSubmittedQueue()
    {
        try
        {
            var reviewerId = GetUserId();
            if (reviewerId == 0)
            {
                return Unauthorized(new ReviewerResponse<List<ReviewQueueItemDto>>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _reviewerService.GetSubmittedQueueAsync(reviewerId);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Reviewer {ReviewerId} retrieved submitted queue", reviewerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submitted review queue");
            return StatusCode(500, new ReviewerResponse<List<ReviewQueueItemDto>>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving review queue"
            });
        }
    }

    /// <summary>
    /// Get one submitted annotation detail with validation checks against project instruction and labels
    /// </summary>
    [HttpGet("review-detail/{annotationId}")]
    [ProducesResponseType(typeof(ReviewerResponse<ReviewDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReviewerResponse<ReviewDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetReviewDetail(long annotationId)
    {
        try
        {
            var reviewerId = GetUserId();
            if (reviewerId == 0)
            {
                return Unauthorized(new ReviewerResponse<ReviewDetailDto>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _reviewerService.GetReviewDetailAsync(reviewerId, annotationId);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Reviewer {ReviewerId} retrieved review detail for annotation {AnnotationId}", reviewerId, annotationId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review detail for annotation {AnnotationId}", annotationId);
            return StatusCode(500, new ReviewerResponse<ReviewDetailDto>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving review detail"
            });
        }
    }

    /// <summary>
    /// Approve annotation or return for rework with comment and error categories
    /// </summary>
    [HttpPost("submit-decision")]
    [ProducesResponseType(typeof(ReviewerResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReviewerResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitDecision([FromBody] SubmitReviewRequest request)
    {
        try
        {
            var reviewerId = GetUserId();
            if (reviewerId == 0)
            {
                return Unauthorized(new ReviewerResponse<string>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            if (request.AnnotationId <= 0)
            {
                return BadRequest(new ReviewerResponse<string>
                {
                    IsSuccess = false,
                    Message = "Valid annotation ID is required"
                });
            }

            var result = await _reviewerService.SubmitReviewDecisionAsync(reviewerId, request);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Reviewer {ReviewerId} submitted decision {Decision} for annotation {AnnotationId}", reviewerId, request.Decision, request.AnnotationId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting review decision for annotation {AnnotationId}", request.AnnotationId);
            return StatusCode(500, new ReviewerResponse<string>
            {
                IsSuccess = false,
                Message = "An error occurred while submitting review decision"
            });
        }
    }
}

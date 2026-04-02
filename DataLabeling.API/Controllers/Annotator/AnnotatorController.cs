using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Annotator;
using DataLabeling.Domain.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DataLabeling.API.Controllers.Annotator;

/// <summary>
/// API endpoints for annotator operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Annotator")]
public class AnnotatorController : ControllerBase
{
    private readonly IAnnotatorService _annotatorService;
    private readonly ILogger<AnnotatorController> _logger;

    public AnnotatorController(IAnnotatorService annotatorService, ILogger<AnnotatorController> logger)
    {
        _annotatorService = annotatorService ?? throw new ArgumentNullException(nameof(annotatorService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private long GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get all assigned labeling tasks for the current annotator
    /// </summary>
    /// <response code="200">List of assigned tasks retrieved successfully</response>
    /// <response code="401">Unauthorized - user not authenticated</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("assigned-tasks")]
    [ProducesResponseType(typeof(AnnotatorResponse<PagedResult<AssignedTaskDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAssignedTasks([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new AnnotatorResponse<PagedResult<AssignedTaskDto>>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _annotatorService.GetAssignedTasksAsync(userId, pageNumber, pageSize);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"Annotator {userId} retrieved assigned tasks (page {pageNumber})");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting assigned tasks: {ex.Message}");
            return StatusCode(500, new AnnotatorResponse<PagedResult<AssignedTaskDto>>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving assigned tasks"
            });
        }
    }

    /// <summary>
    /// Get detailed information about a specific task including instructions and available labels
    /// </summary>
    /// <param name="dataItemAssignmentId">The ID of the data item assignment</param>
    /// <response code="200">Task details retrieved successfully</response>
    /// <response code="400">Invalid request or task not found</response>
    /// <response code="401">Unauthorized - user not authenticated</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("task-detail/{dataItemAssignmentId}")]
    [ProducesResponseType(typeof(AnnotatorResponse<TaskDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AnnotatorResponse<TaskDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTaskDetail(long dataItemAssignmentId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new AnnotatorResponse<TaskDetailDto>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _annotatorService.GetTaskDetailAsync(userId, dataItemAssignmentId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"Annotator {userId} retrieved task detail for assignment {dataItemAssignmentId}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting task detail: {ex.Message}");
            return StatusCode(500, new AnnotatorResponse<TaskDetailDto>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving task details"
            });
        }
    }

    /// <summary>
    /// Create or update annotation for a data item
    /// </summary>
    /// <param name="request">Annotation creation request with label value</param>
    /// <response code="200">Annotation created or updated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized - user not authenticated</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("create-annotation")]
    [ProducesResponseType(typeof(AnnotatorResponse<AnnotationDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AnnotatorResponse<AnnotationDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrUpdateAnnotation([FromBody] CreateAnnotationRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new AnnotatorResponse<AnnotationDetailDto>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new AnnotatorResponse<AnnotationDetailDto>
                {
                    IsSuccess = false,
                    Message = "Invalid request data"
                });
            }

            var result = await _annotatorService.CreateOrUpdateAnnotationAsync(userId, request);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"Annotator {userId} created/updated annotation for data item {request.DataItemId}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating annotation: {ex.Message}");
            return StatusCode(500, new AnnotatorResponse<AnnotationDetailDto>
            {
                IsSuccess = false,
                Message = "An error occurred while creating annotation"
            });
        }
    }

    /// <summary>
    /// Submit annotation for review by a reviewer
    /// </summary>
    /// <param name="request">Submit annotation request with annotation ID</param>
    /// <response code="200">Annotation submitted successfully</response>
    /// <response code="400">Invalid request or annotation not found</response>
    /// <response code="401">Unauthorized - user not authenticated</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("submit-for-review")]
    [ProducesResponseType(typeof(AnnotatorResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AnnotatorResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitForReview([FromBody] SubmitAnnotationRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new AnnotatorResponse<string>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            if (request.AnnotationId <= 0)
            {
                return BadRequest(new AnnotatorResponse<string>
                {
                    IsSuccess = false,
                    Message = "Valid annotation ID is required"
                });
            }

            var result = await _annotatorService.SubmitAnnotationForReviewAsync(userId, request);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"Annotator {userId} submitted annotation {request.AnnotationId} for review");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error submitting annotation: {ex.Message}");
            return StatusCode(500, new AnnotatorResponse<string>
            {
                IsSuccess = false,
                Message = "An error occurred while submitting annotation"
            });
        }
    }

    /// <summary>
    /// Get feedback on a submitted annotation
    /// </summary>
    /// <param name="annotationId">The ID of the annotation</param>
    /// <response code="200">Annotation feedback retrieved successfully</response>
    /// <response code="400">Annotation not found</response>
    /// <response code="401">Unauthorized - user not authenticated</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("annotation-feedback/{annotationId}")]
    [ProducesResponseType(typeof(AnnotatorResponse<AnnotationDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AnnotatorResponse<AnnotationDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAnnotationFeedback(long annotationId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new AnnotatorResponse<AnnotationDetailDto>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _annotatorService.GetAnnotationWithFeedbackAsync(userId, annotationId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"Annotator {userId} retrieved feedback for annotation {annotationId}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting annotation feedback: {ex.Message}");
            return StatusCode(500, new AnnotatorResponse<AnnotationDetailDto>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving annotation feedback"
            });
        }
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetCurrentAnnotatorProfile()
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new AnnotatorResponse<AnnotatorProfileDto>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _annotatorService.GetAnnotatorProfileAsync(userId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"Annotator {userId} retrieved their profile information");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting annotator profile: {ex.Message}");
            return StatusCode(500, new AnnotatorResponse<AnnotatorProfileDto>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving annotator profile"
            });
        }
    }

    /// <summary>
    /// Get annotation history for the current annotator with pagination and optional status filter
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="status">Optional status filter (pending, approved, rejected, etc.)</param>
    /// <response code="200">Annotation history retrieved successfully</response>
    /// <response code="401">Unauthorized - user not authenticated</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("annotation-history")]
    [ProducesResponseType(typeof(AnnotatorResponse<PagedResult<AnnotationHistoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAnnotationHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0)
            {
                return Unauthorized(new AnnotatorResponse<PagedResult<AnnotationHistoryDto>>
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _annotatorService.GetAnnotationHistoryAsync(userId, pageNumber, pageSize, status);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"Annotator {userId} retrieved annotation history (page {pageNumber}, size {pageSize})");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting annotation history: {ex.Message}");
            return StatusCode(500, new AnnotatorResponse<PagedResult<AnnotationHistoryDto>>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving annotation history"
            });
        }
    }
}

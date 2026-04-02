using System.Text.Json;
using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Reviewer;
using DataLabeling.Domain.DTOs.Common;
using DataLabeling.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataLabeling.Application.Services;

/// <summary>
/// Service for handling reviewer-related operations
/// </summary>
public class ReviewerService : IReviewerService
{
    private readonly DataLabelingDBContext _dbContext;

    public ReviewerService(DataLabelingDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReviewerResponse<PagedResult<ReviewQueueItemDto>>> GetSubmittedQueueAsync(long reviewerId, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            var reviewerDatasetIds = await _dbContext.DatasetAssignments
                .Where(d => d.UserId == reviewerId && d.Role == "Reviewer")
                .Select(d => d.DatasetId)
                .Distinct()
                .ToListAsync();

            var query = _dbContext.Annotations
                .Where(a => a.Status == "Submitted" && reviewerDatasetIds.Contains(a.DataItem.DatasetId))
                .Include(a => a.DataItem)
                .ThenInclude(di => di.Dataset)
                .ThenInclude(ds => ds.Project)
                .Include(a => a.User);

            var total = await query.CountAsync();
            var queue = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ReviewQueueItemDto
                {
                    AnnotationId = a.Id,
                    DataItemId = a.DataItemId,
                    DatasetId = a.DataItem.DatasetId,
                    ProjectId = a.DataItem.Dataset.ProjectId,
                    ProjectName = a.DataItem.Dataset.Project.Name,
                    DatasetName = a.DataItem.Dataset.Name,
                    DataContent = a.DataItem.Content,
                    LabelValue = a.LabelValue,
                    AnnotationType = a.AnnotationType,
                    CoordinateData = a.CoordinateData,
                    AnnotationStatus = a.Status,
                    AnnotatorId = a.UserId,
                    AnnotatorName = a.User.Username,
                    SubmittedAt = a.CreatedAt
                })
                .ToListAsync();

            return new ReviewerResponse<PagedResult<ReviewQueueItemDto>>
            {
                IsSuccess = true,
                Message = $"Retrieved {queue.Count} submitted annotations",
                Data = new PagedResult<ReviewQueueItemDto> { Items = queue, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize }
            };
        }
        catch (Exception ex)
        {
            return new ReviewerResponse<PagedResult<ReviewQueueItemDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving review queue: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ReviewerResponse<ReviewDetailDto>> GetReviewDetailAsync(long reviewerId, long annotationId)
    {
        try
        {
            var annotation = await _dbContext.Annotations
                .Where(a => a.Id == annotationId)
                .Include(a => a.DataItem)
                .ThenInclude(di => di.Dataset)
                .ThenInclude(ds => ds.Project)
                .Include(a => a.User)
                .FirstOrDefaultAsync();

            if (annotation == null)
            {
                return new ReviewerResponse<ReviewDetailDto>
                {
                    IsSuccess = false,
                    Message = "Annotation not found",
                    Data = null
                };
            }

            var hasAccess = await IsReviewerAssignedToDatasetAsync(reviewerId, annotation.DataItem.DatasetId);
            if (!hasAccess)
            {
                return new ReviewerResponse<ReviewDetailDto>
                {
                    IsSuccess = false,
                    Message = "You do not have access to review this annotation",
                    Data = null
                };
            }

            var allowedLabels = await _dbContext.Labels
                .Where(l => l.ProjectId == annotation.DataItem.Dataset.ProjectId)
                .Select(l => l.Name)
                .ToListAsync();

            var validation = ValidateAnnotation(annotation.LabelValue, annotation.AnnotationType, annotation.CoordinateData, allowedLabels);

            var detail = new ReviewDetailDto
            {
                AnnotationId = annotation.Id,
                DataItemId = annotation.DataItemId,
                DatasetId = annotation.DataItem.DatasetId,
                ProjectId = annotation.DataItem.Dataset.ProjectId,
                ProjectName = annotation.DataItem.Dataset.Project.Name,
                DatasetName = annotation.DataItem.Dataset.Name,
                DataContent = annotation.DataItem.Content,
                ProjectInstruction = annotation.DataItem.Dataset.Project.Description,
                LabelValue = annotation.LabelValue,
                AnnotationType = annotation.AnnotationType,
                CoordinateData = annotation.CoordinateData,
                AnnotatorId = annotation.UserId,
                AnnotatorName = annotation.User.Username,
                SubmittedAt = annotation.CreatedAt,
                AllowedLabels = allowedLabels,
                Validation = validation
            };

            return new ReviewerResponse<ReviewDetailDto>
            {
                IsSuccess = true,
                Message = "Review detail retrieved successfully",
                Data = detail
            };
        }
        catch (Exception ex)
        {
            return new ReviewerResponse<ReviewDetailDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving review detail: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<ReviewerResponse<string>> SubmitReviewDecisionAsync(long reviewerId, SubmitReviewRequest request)
    {
        try
        {
            var decision = NormalizeDecision(request.Decision);
            if (decision == null)
            {
                return new ReviewerResponse<string>
                {
                    IsSuccess = false,
                    Message = "Decision must be Approved or NeedsRework",
                    Data = null
                };
            }

            var annotation = await _dbContext.Annotations
                .Where(a => a.Id == request.AnnotationId)
                .Include(a => a.DataItem)
                .FirstOrDefaultAsync();

            if (annotation == null)
            {
                return new ReviewerResponse<string>
                {
                    IsSuccess = false,
                    Message = "Annotation not found",
                    Data = null
                };
            }

            if (!string.Equals(annotation.Status, "Submitted", StringComparison.OrdinalIgnoreCase))
            {
                return new ReviewerResponse<string>
                {
                    IsSuccess = false,
                    Message = "Only submitted annotations can be reviewed",
                    Data = null
                };
            }

            var hasAccess = await IsReviewerAssignedToDatasetAsync(reviewerId, annotation.DataItem.DatasetId);
            if (!hasAccess)
            {
                return new ReviewerResponse<string>
                {
                    IsSuccess = false,
                    Message = "You do not have access to review this annotation",
                    Data = null
                };
            }

            var cleanedCategories = request.ErrorCategories
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (decision == "rejected" && string.IsNullOrWhiteSpace(request.Comment) && cleanedCategories.Count == 0)
            {
                return new ReviewerResponse<string>
                {
                    IsSuccess = false,
                    Message = "Comment or error categories are required when rejecting annotation for rework",
                    Data = null
                };
            }

            var commentPayload = new ReviewCommentPayload
            {
                Comment = request.Comment,
                ErrorCategories = cleanedCategories
            };

            var review = new Review
            {
                AnnotationId = annotation.Id,
                ReviewerId = reviewerId,
                Status = decision,
                Comment = JsonSerializer.Serialize(commentPayload)
                // ReviewedAt will be set by database default
            };

            _dbContext.Reviews.Add(review);

            // Update annotation status based on review decision
            if (decision == "approved")
            {
                annotation.Status = "approved";
                
                // Create final result record when annotation is approved
                var finalResult = new FinalResult
                {
                    DataItemId = annotation.DataItemId,
                    AnnotationId = annotation.Id,
                    DecidedBy = reviewerId,
                    DecidedAt = DateTime.UtcNow
                };
                _dbContext.FinalResults.Add(finalResult);
            }
            else if (decision == "rejected")
            {
                // Send back to annotator for revision
                annotation.Status = "need_rework";
            }
            
            _dbContext.Annotations.Update(annotation);

            await _dbContext.SaveChangesAsync();

            return new ReviewerResponse<string>
            {
                IsSuccess = true,
                Message = "Review decision submitted successfully",
                Data = $"Annotation {annotation.Id} updated to {decision}"
            };
        }
        catch (Exception ex)
        {
            var message = $"Error submitting review decision: {ex.Message}";
            if (ex.InnerException != null)
            {
                message += $" | Inner: {ex.InnerException.Message}";
            }
            return new ReviewerResponse<string>
            {
                IsSuccess = false,
                Message = message,
                Data = null
            };
        }
    }

    private async Task<bool> IsReviewerAssignedToDatasetAsync(long reviewerId, long datasetId)
    {
        return await _dbContext.DatasetAssignments
            .AnyAsync(d => d.UserId == reviewerId && d.DatasetId == datasetId && d.Role == "Reviewer");
    }

    private static string? NormalizeDecision(string? decision)
    {
        if (string.IsNullOrWhiteSpace(decision))
        {
            return null;
        }

        return decision.Trim().ToLowerInvariant() switch
        {
            "approved" => "approved",
            "needsrework" => "rejected",
            "rework" => "rejected",
            "rejected" => "rejected",
            _ => null
        };
    }

    private static ValidationResultDto ValidateAnnotation(string? labelValue, string? annotationType, string? coordinateData, List<string> allowedLabels)
    {
        var result = new ValidationResultDto
        {
            IsValid = true,
            LabelExistsInProject = true,
            IsCoordinateJsonValid = true
        };

        if (string.IsNullOrWhiteSpace(labelValue))
        {
            result.IsValid = false;
            result.LabelExistsInProject = false;
            result.Issues.Add("Label value is empty.");
        }
        else
        {
            var allowed = allowedLabels.Any(x => string.Equals(x, labelValue, StringComparison.OrdinalIgnoreCase));
            result.LabelExistsInProject = allowed;
            if (!allowed)
            {
                result.IsValid = false;
                result.Issues.Add("Label does not exist in project label definitions.");
            }
        }

        if (string.IsNullOrWhiteSpace(coordinateData))
        {
            return result;
        }

        if (!TryValidateCoordinateData(annotationType, coordinateData, out var issue))
        {
            result.IsValid = false;
            result.IsCoordinateJsonValid = false;
            result.Issues.Add(issue);
        }

        return result;
    }

    private static bool TryValidateCoordinateData(string? annotationType, string coordinateData, out string issue)
    {
        issue = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(coordinateData);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                issue = "CoordinateData must be a JSON object.";
                return false;
            }

            var normalizedType = annotationType?.Trim().ToLowerInvariant();

            if (normalizedType == "bbox")
            {
                return EnsureNumber(root, "x1", out issue)
                    && EnsureNumber(root, "y1", out issue)
                    && EnsureNumber(root, "x2", out issue)
                    && EnsureNumber(root, "y2", out issue);
            }

            if (normalizedType == "point")
            {
                return EnsureNumber(root, "x", out issue)
                    && EnsureNumber(root, "y", out issue);
            }

            if (normalizedType == "polygon")
            {
                if (!root.TryGetProperty("points", out var points) || points.ValueKind != JsonValueKind.Array)
                {
                    issue = "Polygon requires points array.";
                    return false;
                }

                if (points.GetArrayLength() < 3)
                {
                    issue = "Polygon requires at least 3 points.";
                    return false;
                }

                foreach (var point in points.EnumerateArray())
                {
                    if (point.ValueKind != JsonValueKind.Array || point.GetArrayLength() != 2)
                    {
                        issue = "Each polygon point must be [x, y].";
                        return false;
                    }

                    var arr = point.EnumerateArray().ToArray();
                    if (arr[0].ValueKind != JsonValueKind.Number || arr[1].ValueKind != JsonValueKind.Number)
                    {
                        issue = "Polygon point values must be numeric.";
                        return false;
                    }
                }
            }

            return true;
        }
        catch (JsonException)
        {
            issue = "CoordinateData is not valid JSON.";
            return false;
        }
    }

    private static bool EnsureNumber(JsonElement root, string propertyName, out string issue)
    {
        issue = string.Empty;

        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            issue = $"Missing or invalid numeric property: {propertyName}.";
            return false;
        }

        return true;
    }

    public async Task<ReviewerResponse<PagedResult<ReviewHistoryDto>>> GetReviewHistoryAsync(long reviewerId, int pageNumber = 1, int pageSize = 20, string? status = null)
    {
        try
        {
            var query = _dbContext.Reviews
                .Where(r => r.ReviewerId == reviewerId)
                .Include(r => r.Annotation)
                .ThenInclude(a => a.DataItem)
                .ThenInclude(di => di.Dataset)
                .ThenInclude(ds => ds.Project)
                .Include(r => r.Annotation)
                .ThenInclude(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var total = await query.CountAsync();

            var reviews = await query
                .OrderByDescending(r => r.ReviewedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var history = reviews.Select(r => new ReviewHistoryDto
            {
                ReviewId = r.Id,
                AnnotationId = r.AnnotationId,
                DataItemId = r.Annotation.DataItemId,
                DatasetId = r.Annotation.DataItem.DatasetId,
                ProjectId = r.Annotation.DataItem.Dataset.ProjectId,
                ProjectName = r.Annotation.DataItem.Dataset.Project.Name,
                DatasetName = r.Annotation.DataItem.Dataset.Name,
                AnnotatorId = r.Annotation.UserId,
                AnnotatorName = r.Annotation.User.Username,
                LabelValue = r.Annotation.LabelValue,
                ReviewStatus = r.Status,
                Comment = r.Comment,
                ReviewedAt = r.ReviewedAt,
                AnnotationSubmittedAt = r.Annotation.CreatedAt
            }).ToList();

            return new ReviewerResponse<PagedResult<ReviewHistoryDto>>
            {
                IsSuccess = true,
                Message = $"Retrieved {history.Count} review history records",
                Data = new PagedResult<ReviewHistoryDto>
                {
                    Items = history,
                    TotalCount = total,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                }
            };
        }
        catch (Exception ex)
        {
            return new ReviewerResponse<PagedResult<ReviewHistoryDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving review history: {ex.Message}",
                Data = null
            };
        }
    }

    private class ReviewCommentPayload
    {
        public string? Comment { get; set; }
        public List<string> ErrorCategories { get; set; } = new();
    }
}


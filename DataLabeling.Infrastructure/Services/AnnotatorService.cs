using Microsoft.EntityFrameworkCore;
using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Annotator;
using DataLabeling.Domain.Models;

namespace DataLabeling.Infrastructure.Services;

/// <summary>
/// Service for handling annotator-related operations
/// </summary>
public class AnnotatorService : IAnnotatorService
{
    private readonly DataLabelingDBContext _dbContext;

    public AnnotatorService(DataLabelingDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all assigned tasks for an annotator
    /// </summary>
    public async Task<AnnotatorResponse<List<AssignedTaskDto>>> GetAssignedTasksAsync(long userId)
    {
        try
        {
            var tasks = await _dbContext.DataItemAssignments
                .Where(d => d.UserId == userId && d.Status != "Completed")
                .Include(d => d.DataItem)
                .ThenInclude(d => d.Dataset)
                .Select(d => new AssignedTaskDto
                {
                    DataItemAssignmentId = d.Id,
                    DataItemId = d.DataItemId,
                    DatasetId = d.DataItem.DatasetId,
                    DatasetName = d.DataItem.Dataset.Name,
                    DataContent = d.DataItem.Content,
                    Status = d.Status,
                    AssignedAt = d.AssignedAt,
                    HasAnnotation = d.DataItem.Annotations.Any(a => a.UserId == userId),
                    CurrentAnnotationStatus = d.DataItem.Annotations
                        .Where(a => a.UserId == userId)
                        .Select(a => a.Status)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return new AnnotatorResponse<List<AssignedTaskDto>>
            {
                IsSuccess = true,
                Message = $"Retrieved {tasks.Count} assigned tasks",
                Data = tasks
            };
        }
        catch (Exception ex)
        {
            return new AnnotatorResponse<List<AssignedTaskDto>>
            {
                IsSuccess = false,
                Message = $"Error retrieving assigned tasks: {ex.Message}",
                Data = null
            };
        }
    }

    /// <summary>
    /// Get detailed information about a task including instructions and available labels
    /// </summary>
    public async Task<AnnotatorResponse<TaskDetailDto>> GetTaskDetailAsync(long userId, long dataItemAssignmentId)
    {
        try
        {
            // Verify the assignment belongs to the user
            var assignment = await _dbContext.DataItemAssignments
                .Where(d => d.Id == dataItemAssignmentId && d.UserId == userId)
                .Include(d => d.DataItem)
                .ThenInclude(d => d.Dataset)
                .ThenInclude(d => d.Project)
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return new AnnotatorResponse<TaskDetailDto>
                {
                    IsSuccess = false,
                    Message = "Task not found or you don't have access to it",
                    Data = null
                };
            }

            var dataItem = assignment.DataItem;
            var project = dataItem.Dataset.Project;

            // Get available labels for this project
            var labels = await _dbContext.Labels
                .Where(l => l.ProjectId == project.Id)
                .Select(l => new LabelOptionDto
                {
                    LabelId = l.Id,
                    LabelName = l.Name,
                    ParentLabelId = l.ParentId,
                    ParentLabelName = l.Parent != null ? l.Parent.Name : null
                })
                .ToListAsync();

            // Get current annotation if exists
            var currentAnnotation = await _dbContext.Annotations
                .Where(a => a.DataItemId == dataItem.Id && a.UserId == userId)
                .Include(a => a.Reviews)
                .ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync();

            AnnotationDetailDto? annotationDto = null;
            if (currentAnnotation != null)
            {
                annotationDto = new AnnotationDetailDto
                {
                    AnnotationId = currentAnnotation.Id,
                    DataItemId = currentAnnotation.DataItemId,
                    LabelValue = currentAnnotation.LabelValue,
                    AnnotationType = currentAnnotation.AnnotationType,
                    CoordinateData = currentAnnotation.CoordinateData,
                    Status = currentAnnotation.Status,
                    CreatedAt = currentAnnotation.CreatedAt,
                    Feedbacks = currentAnnotation.Reviews
                        .Select(r => new AnnotationFeedbackDto
                        {
                            ReviewId = r.Id,
                            AnnotationId = r.AnnotationId,
                            ReviewStatus = r.Status,
                            Comment = r.Comment,
                            ReviewerName = r.Reviewer.Username,
                            ReviewedAt = r.ReviewedAt
                        })
                        .ToList()
                };
            }

            var taskDetail = new TaskDetailDto
            {
                DataItemAssignmentId = assignment.Id,
                DataItemId = dataItem.Id,
                DatasetId = dataItem.DatasetId,
                ProjectId = project.Id,
                DatasetName = dataItem.Dataset.Name,
                ProjectName = project.Name,
                DataContent = dataItem.Content,
                ProjectDescription = project.Description, // Use as instructions
                AssignedAt = assignment.AssignedAt,
                AvailableLabels = labels,
                CurrentAnnotation = annotationDto
            };

            return new AnnotatorResponse<TaskDetailDto>
            {
                IsSuccess = true,
                Message = "Task details retrieved successfully",
                Data = taskDetail
            };
        }
        catch (Exception ex)
        {
            return new AnnotatorResponse<TaskDetailDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving task details: {ex.Message}",
                Data = null
            };
        }
    }

    /// <summary>
    /// Create or update annotation for a data item
    /// </summary>
    public async Task<AnnotatorResponse<AnnotationDetailDto>> CreateOrUpdateAnnotationAsync(long userId, CreateAnnotationRequest request)
    {
        try
        {
            // Verify user has access to this data item
            var assignment = await _dbContext.DataItemAssignments
                .Where(d => d.Id == request.DataItemAssignmentId && d.UserId == userId)
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return new AnnotatorResponse<AnnotationDetailDto>
                {
                    IsSuccess = false,
                    Message = "You don't have access to this task",
                    Data = null
                };
            }

            // Check if annotation already exists
            var existingAnnotation = await _dbContext.Annotations
                .Where(a => a.DataItemId == request.DataItemId && a.UserId == userId)
                .FirstOrDefaultAsync();

            Annotation annotation;

            if (existingAnnotation != null)
            {
                // Update existing annotation
                existingAnnotation.LabelValue = request.LabelValue;
                existingAnnotation.AnnotationType = request.AnnotationType;
                existingAnnotation.CoordinateData = request.CoordinateData;
                existingAnnotation.Status = "InProgress";
                _dbContext.Annotations.Update(existingAnnotation);
                annotation = existingAnnotation;
            }
            else
            {
                // Create new annotation
                annotation = new Annotation
                {
                    DataItemId = request.DataItemId,
                    UserId = userId,
                    LabelValue = request.LabelValue,
                    AnnotationType = request.AnnotationType,
                    CoordinateData = request.CoordinateData,
                    Status = "InProgress",
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.Annotations.Add(annotation);
            }

            await _dbContext.SaveChangesAsync();

            var annotationDto = new AnnotationDetailDto
            {
                AnnotationId = annotation.Id,
                DataItemId = annotation.DataItemId,
                LabelValue = annotation.LabelValue,
                AnnotationType = annotation.AnnotationType,
                CoordinateData = annotation.CoordinateData,
                Status = annotation.Status,
                CreatedAt = annotation.CreatedAt
            };

            return new AnnotatorResponse<AnnotationDetailDto>
            {
                IsSuccess = true,
                Message = existingAnnotation != null ? "Annotation updated successfully" : "Annotation created successfully",
                Data = annotationDto
            };
        }
        catch (Exception ex)
        {
            return new AnnotatorResponse<AnnotationDetailDto>
            {
                IsSuccess = false,
                Message = $"Error creating/updating annotation: {ex.Message}",
                Data = null
            };
        }
    }

    /// <summary>
    /// Submit annotation for review
    /// </summary>
    public async Task<AnnotatorResponse<string>> SubmitAnnotationForReviewAsync(long userId, SubmitAnnotationRequest request)
    {
        try
        {
            var annotation = await _dbContext.Annotations
                .Where(a => a.Id == request.AnnotationId && a.UserId == userId)
                .FirstOrDefaultAsync();

            if (annotation == null)
            {
                return new AnnotatorResponse<string>
                {
                    IsSuccess = false,
                    Message = "Annotation not found or you don't own this annotation",
                    Data = null
                };
            }

            if (string.IsNullOrWhiteSpace(annotation.LabelValue))
            {
                return new AnnotatorResponse<string>
                {
                    IsSuccess = false,
                    Message = "Cannot submit annotation without a label value",
                    Data = null
                };
            }

            annotation.Status = "Submitted";
            _dbContext.Annotations.Update(annotation);
            await _dbContext.SaveChangesAsync();

            return new AnnotatorResponse<string>
            {
                IsSuccess = true,
                Message = "Annotation submitted for review successfully",
                Data = $"Annotation {request.AnnotationId} submitted for review"
            };
        }
        catch (Exception ex)
        {
            return new AnnotatorResponse<string>
            {
                IsSuccess = false,
                Message = $"Error submitting annotation: {ex.Message}",
                Data = null
            };
        }
    }

    /// <summary>
    /// Get feedback on an annotation
    /// </summary>
    public async Task<AnnotatorResponse<AnnotationDetailDto>> GetAnnotationWithFeedbackAsync(long userId, long annotationId)
    {
        try
        {
            var annotation = await _dbContext.Annotations
                .Where(a => a.Id == annotationId && a.UserId == userId)
                .Include(a => a.Reviews)
                .ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync();

            if (annotation == null)
            {
                return new AnnotatorResponse<AnnotationDetailDto>
                {
                    IsSuccess = false,
                    Message = "Annotation not found or you don't have access to it",
                    Data = null
                };
            }

            var feedbacks = annotation.Reviews
                .Select(r => new AnnotationFeedbackDto
                {
                    ReviewId = r.Id,
                    AnnotationId = r.AnnotationId,
                    ReviewStatus = r.Status,
                    Comment = r.Comment,
                    ReviewerName = r.Reviewer.Username,
                    ReviewedAt = r.ReviewedAt
                })
                .ToList();

            var annotationDto = new AnnotationDetailDto
            {
                AnnotationId = annotation.Id,
                DataItemId = annotation.DataItemId,
                LabelValue = annotation.LabelValue,
                AnnotationType = annotation.AnnotationType,
                CoordinateData = annotation.CoordinateData,
                Status = annotation.Status,
                CreatedAt = annotation.CreatedAt,
                Feedbacks = feedbacks
            };

            return new AnnotatorResponse<AnnotationDetailDto>
            {
                IsSuccess = true,
                Message = "Annotation with feedback retrieved successfully",
                Data = annotationDto
            };
        }
        catch (Exception ex)
        {
            return new AnnotatorResponse<AnnotationDetailDto>
            {
                IsSuccess = false,
                Message = $"Error retrieving annotation: {ex.Message}",
                Data = null
            };
        }
    }
}

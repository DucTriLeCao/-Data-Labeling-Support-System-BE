using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Common;
using DataLabeling.Domain.DTOs.Manager;
using DataLabeling.Domain.DTOs.Admin.UserMgmt;
using DataLabeling.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataLabeling.Application.Services;

public class ManagerService : IManagerService
{
    private readonly DataLabelingDBContext _context;

    public ManagerService(DataLabelingDBContext context)
    {
        _context = context;
    }

    #region Projects
    public async Task<PagedResult<ProjectDto>> GetProjectsAsync(int pageNumber, int pageSize, string? status = null, string? searchTerm = null)
    {
        var query = _context.Projects.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Name.Contains(searchTerm));
        }

        var total = await query.CountAsync();
        var data = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<ProjectDto> { Items = data, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<ProjectDto?> GetProjectByIdAsync(long id)
    {
        var p = await _context.Projects.FindAsync(id);
        if (p == null) return null;
        return new ProjectDto { Id = p.Id, Name = p.Name, Description = p.Description, Status = p.Status, CreatedAt = p.CreatedAt };
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto request)
    {
        if (await _context.Projects.AnyAsync(p => p.Name == request.Name))
            throw new InvalidOperationException("Project name already exists.");

        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return new ProjectDto { Id = project.Id, Name = project.Name, Description = project.Description, Status = project.Status, CreatedAt = project.CreatedAt };
    }

    public async Task<ProjectDto> UpdateProjectAsync(long id, UpdateProjectDto request)
    {
        var p = await _context.Projects.FindAsync(id);
        if (p == null) throw new KeyNotFoundException($"Project {id} not found.");

        if (!string.IsNullOrWhiteSpace(request.Name)) p.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Description)) p.Description = request.Description;
        if (!string.IsNullOrWhiteSpace(request.Status)) p.Status = request.Status;

        _context.Projects.Update(p);
        await _context.SaveChangesAsync();
        return new ProjectDto { Id = p.Id, Name = p.Name, Description = p.Description, Status = p.Status, CreatedAt = p.CreatedAt };
    }

    public async Task<bool> DeleteProjectAsync(long id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return false;

        var datasets = await _context.Datasets.Where(d => d.ProjectId == id).ToListAsync();
        foreach (var dataset in datasets)
        {
            var dataItems = await _context.DataItems.Where(di => di.DatasetId == dataset.Id).ToListAsync();
            _context.DataItems.RemoveRange(dataItems);
            _context.Datasets.Remove(dataset);
        }

        var members = await _context.ProjectMembers.Where(pm => pm.ProjectId == id).ToListAsync();
        _context.ProjectMembers.RemoveRange(members);

        var labels = await _context.Labels.Where(l => l.ProjectId == id).ToListAsync();
        _context.Labels.RemoveRange(labels);

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<BulkDeleteResultDto> BulkDeleteProjectsAsync(List<long> projectIds)
    {
        var result = new BulkDeleteResultDto { TotalRequested = projectIds.Count };

        foreach (var projectId in projectIds)
        {
            try
            {
                if (await DeleteProjectAsync(projectId))
                {
                    result.SuccessfullyDeleted++;
                }
                else
                {
                    result.Failed++;
                    result.Errors.Add(new BulkDeleteErrorDto { Id = projectId, Reason = "Project not found" });
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add(new BulkDeleteErrorDto { Id = projectId, Reason = ex.Message });
            }
        }

        return result;
    }
    #endregion

    #region Datasets
    public async Task<PagedResult<DatasetDto>> GetDatasetsAsync(long projectId, int pageNumber, int pageSize, string? status = null, string? searchTerm = null)
    {
        var query = _context.Datasets.Where(d => d.ProjectId == projectId).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(d => d.Status == status);
        if (!string.IsNullOrWhiteSpace(searchTerm)) query = query.Where(d => d.Name.Contains(searchTerm));

        var total = await query.CountAsync();
        var data = await query.OrderByDescending(d => d.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DatasetDto
            {
                Id = d.Id, ProjectId = d.ProjectId, Name = d.Name, Description = d.Description, Status = d.Status, CreatedAt = d.CreatedAt
            }).ToListAsync();

        return new PagedResult<DatasetDto> { Items = data, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<DatasetDto?> GetDatasetByIdAsync(long id)
    {
        var dataset = await _context.Datasets.FirstOrDefaultAsync(d => d.Id == id);
        if (dataset == null) return null;

        return new DatasetDto
        {
            Id = dataset.Id,
            ProjectId = dataset.ProjectId,
            Name = dataset.Name,
            Description = dataset.Description,
            Status = dataset.Status,
            CreatedAt = dataset.CreatedAt
        };
    }

    public async Task<DatasetDto> CreateDatasetAsync(long projectId, CreateDatasetDto request)
    {
        if (!await _context.Projects.AnyAsync(p => p.Id == projectId)) throw new KeyNotFoundException("Project not found.");

        var ds = new Dataset
        {
            ProjectId = projectId,
            Name = request.Name,
            Description = request.Description,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };
        _context.Datasets.Add(ds);
        await _context.SaveChangesAsync();
        return new DatasetDto { Id = ds.Id, ProjectId = ds.ProjectId, Name = ds.Name, Description = ds.Description, Status = ds.Status, CreatedAt = ds.CreatedAt };
    }

    public async Task<DatasetDto> UpdateDatasetAsync(long id, UpdateDatasetDto request)
    {
        var d = await _context.Datasets.FindAsync(id);
        if (d == null) throw new KeyNotFoundException($"Dataset {id} not found.");

        if (!string.IsNullOrWhiteSpace(request.Name)) d.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Description)) d.Description = request.Description;
        if (!string.IsNullOrWhiteSpace(request.Status)) d.Status = request.Status;

        _context.Datasets.Update(d);
        await _context.SaveChangesAsync();
        return new DatasetDto { Id = d.Id, ProjectId = d.ProjectId, Name = d.Name, Description = d.Description, Status = d.Status, CreatedAt = d.CreatedAt };
    }

    public async Task<bool> DeleteDatasetAsync(long id)
    {
        var dataset = await _context.Datasets.FindAsync(id);
        if (dataset == null) return false;

        // Delete all data items in dataset
        var dataItems = await _context.DataItems.Where(di => di.DatasetId == id).ToListAsync();
        _context.DataItems.RemoveRange(dataItems);

        // Delete dataset assignments
        var assignments = await _context.DatasetAssignments.Where(da => da.DatasetId == id).ToListAsync();
        _context.DatasetAssignments.RemoveRange(assignments);

        _context.Datasets.Remove(dataset);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<BulkDeleteResultDto> BulkDeleteDatasetsAsync(List<long> datasetIds)
    {
        var result = new BulkDeleteResultDto { TotalRequested = datasetIds.Count };

        foreach (var datasetId in datasetIds)
        {
            try
            {
                if (await DeleteDatasetAsync(datasetId))
                {
                    result.SuccessfullyDeleted++;
                }
                else
                {
                    result.Failed++;
                    result.Errors.Add(new BulkDeleteErrorDto { Id = datasetId, Reason = "Dataset not found" });
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add(new BulkDeleteErrorDto { Id = datasetId, Reason = ex.Message });
            }
        }

        return result;
    }

    public async Task<PagedResult<DataItemDto>> GetDataItemsAsync(long datasetId, int pageNumber, int pageSize, string? status = null, string? searchTerm = null)
    {
        var query = _context.DataItems.Where(di => di.DatasetId == datasetId).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(di => di.Status == status);
        if (!string.IsNullOrWhiteSpace(searchTerm)) query = query.Where(di => di.Content.Contains(searchTerm));

        var total = await query.CountAsync();
        var data = await query.OrderByDescending(di => di.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(di => new DataItemDto
            {
                Id = di.Id,
                DatasetId = di.DatasetId,
                Content = di.Content,
                Status = di.Status,
                CreatedAt = di.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<DataItemDto> { Items = data, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<DataItemDto?> GetDataItemByIdAsync(long id)
    {
        var dataItem = await _context.DataItems.FirstOrDefaultAsync(di => di.Id == id);
        if (dataItem == null) return null;

        return new DataItemDto
        {
            Id = dataItem.Id,
            DatasetId = dataItem.DatasetId,
            Content = dataItem.Content,
            Status = dataItem.Status,
            CreatedAt = dataItem.CreatedAt
        };
    }

    public async Task<DataItemDto> CreateDataItemAsync(long datasetId, Stream fileStream, string fileName)
    {
        if (!await _context.Datasets.AnyAsync(d => d.Id == datasetId))
            throw new KeyNotFoundException("Dataset not found.");

        // Validate file
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException($"File type '{extension}' is not supported. Allowed: {string.Join(", ", allowedExtensions)}");

        // Save file to wwwroot/uploads/datasets/{datasetId}/
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "datasets", datasetId.ToString());
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(stream);
        }

        // Store the relative URL in Content
        var imageUrl = $"/uploads/datasets/{datasetId}/{uniqueFileName}";

        var dataItem = new DataItem
        {
            DatasetId = datasetId,
            Content = imageUrl,
            Status = "unassigned",
            CreatedAt = DateTime.UtcNow
        };

        _context.DataItems.Add(dataItem);
        await _context.SaveChangesAsync();

        return new DataItemDto
        {
            Id = dataItem.Id,
            DatasetId = dataItem.DatasetId,
            Content = dataItem.Content,
            Status = dataItem.Status,
            CreatedAt = dataItem.CreatedAt
        };
    }

    public async Task<bool> DeleteDataItemAsync(long id)
    {
        var dataItem = await _context.DataItems.FindAsync(id);
        if (dataItem == null) return false;

        // Delete annotations for this data item
        var annotations = await _context.Annotations.Where(a => a.DataItemId == id).ToListAsync();
        _context.Annotations.RemoveRange(annotations);

        // Delete data item assignments
        var assignments = await _context.DataItemAssignments.Where(dia => dia.DataItemId == id).ToListAsync();
        _context.DataItemAssignments.RemoveRange(assignments);

        _context.DataItems.Remove(dataItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<BulkDeleteResultDto> BulkDeleteDataItemsAsync(List<long> dataItemIds)
    {
        var result = new BulkDeleteResultDto { TotalRequested = dataItemIds.Count };

        foreach (var dataItemId in dataItemIds)
        {
            try
            {
                if (await DeleteDataItemAsync(dataItemId))
                {
                    result.SuccessfullyDeleted++;
                }
                else
                {
                    result.Failed++;
                    result.Errors.Add(new BulkDeleteErrorDto { Id = dataItemId, Reason = "Data item not found" });
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add(new BulkDeleteErrorDto { Id = dataItemId, Reason = ex.Message });
            }
        }

        return result;
    }
    #endregion

    #region Labels
    public async Task<PagedResult<LabelDto>> GetLabelsAsync(long projectId, int pageNumber, int pageSize)
    {
        var query = _context.Labels.Where(l => l.ProjectId == projectId).AsQueryable();
        var total = await query.CountAsync();
        var data = await query.OrderBy(l => l.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LabelDto { Id = l.Id, ProjectId = l.ProjectId, Name = l.Name, ParentId = l.ParentId })
            .ToListAsync();
        return new PagedResult<LabelDto> { Items = data, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<LabelDto?> GetLabelByIdAsync(long id)
    {
        var label = await _context.Labels.FindAsync(id);
        if (label == null) return null;
        return new LabelDto { Id = label.Id, ProjectId = label.ProjectId, Name = label.Name, ParentId = label.ParentId };
    }

    public async Task<LabelDto> CreateLabelAsync(long projectId, CreateLabelDto request)
    {
        if (!await _context.Projects.AnyAsync(p => p.Id == projectId)) throw new KeyNotFoundException("Project not found.");

        // Treat 0 as null (root label with no parent)
        long? parentId = request.ParentId is null or 0 ? null : request.ParentId;

        // Validate parent exists if specified
        if (parentId.HasValue && !await _context.Labels.AnyAsync(l => l.Id == parentId.Value))
            throw new KeyNotFoundException($"Parent label {parentId.Value} not found.");

        var label = new Label
        {
            ProjectId = projectId,
            Name = request.Name,
            ParentId = parentId
        };
        _context.Labels.Add(label);
        await _context.SaveChangesAsync();
        return new LabelDto { Id = label.Id, ProjectId = label.ProjectId, Name = label.Name, ParentId = label.ParentId };
    }

    public async Task<LabelDto> UpdateLabelAsync(long id, UpdateLabelDto request)
    {
        var l = await _context.Labels.FindAsync(id);
        if (l == null) throw new KeyNotFoundException($"Label {id} not found.");

        if (!string.IsNullOrWhiteSpace(request.Name)) l.Name = request.Name;
        if (request.ParentId.HasValue) l.ParentId = request.ParentId;

        _context.Labels.Update(l);
        await _context.SaveChangesAsync();
        return new LabelDto { Id = l.Id, ProjectId = l.ProjectId, Name = l.Name, ParentId = l.ParentId };
    }

    public async Task<bool> DeleteLabelAsync(long id)
    {
        var l = await _context.Labels.FindAsync(id);
        if (l == null) return false;
        
        // Delete all child labels recursively
        var childLabels = await _context.Labels.Where(child => child.ParentId == id).ToListAsync();
        foreach (var child in childLabels)
        {
            await DeleteLabelAsync(child.Id);
        }

        _context.Labels.Remove(l);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<BulkDeleteResultDto> BulkDeleteLabelsAsync(List<long> labelIds)
    {
        var result = new BulkDeleteResultDto { TotalRequested = labelIds.Count };

        foreach (var labelId in labelIds)
        {
            try
            {
                if (await DeleteLabelAsync(labelId))
                {
                    result.SuccessfullyDeleted++;
                }
                else
                {
                    result.Failed++;
                    result.Errors.Add(new BulkDeleteErrorDto { Id = labelId, Reason = "Label not found" });
                }
            }
            catch (InvalidOperationException ex)
            {
                result.Failed++;
                result.Errors.Add(new BulkDeleteErrorDto { Id = labelId, Reason = ex.Message });
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add(new BulkDeleteErrorDto { Id = labelId, Reason = ex.Message });
            }
        }

        return result;
    }
    #endregion

    #region Assignment & Progress
    public async Task<DatasetProgressDto> GetDatasetProgressAsync(long datasetId)
    {
        if (!await _context.Datasets.AnyAsync(d => d.Id == datasetId)) throw new KeyNotFoundException("Dataset not found.");

        // Instead of hard-counting large tables in complex ways, we do a basic projection using grouping.
        var dataItems = await _context.DataItems.Where(di => di.DatasetId == datasetId).ToListAsync();
        
        int total = dataItems.Count;
        int annotated = dataItems.Count(di => di.Status == "annotated" || di.Status == "submitted");
        int reviewed = dataItems.Count(di => di.Status == "approved" || di.Status == "rejected");
        int pending = total - annotated - reviewed;

        return new DatasetProgressDto
        {
            DatasetId = datasetId,
            TotalDataItems = total,
            AnnotatedItems = annotated,
            ReviewedItems = reviewed,
            PendingItems = pending
        };
    }

    public async Task<List<DatasetAssignmentDto>> GetDatasetAssignmentsAsync(long datasetId)
    {
        // Verify dataset exists
        if (!await _context.Datasets.AnyAsync(d => d.Id == datasetId))
            throw new KeyNotFoundException($"Dataset {datasetId} not found.");

        var assignments = await _context.DatasetAssignments
            .Where(da => da.DatasetId == datasetId)
            .Include(da => da.User)
            .OrderBy(da => da.Role)
            .ThenBy(da => da.User.Username)
            .Select(da => new DatasetAssignmentDto
            {
                Id = da.Id,
                DatasetId = da.DatasetId,
                UserId = da.UserId,
                Username = da.User.Username,
                Role = da.Role,
                AssignedAt = da.AssignedAt
            })
            .ToListAsync();

        return assignments;
    }

    // Assign individual data items to users (new primary assignment method)
    public async Task<bool> AssignDataItemsAsync(List<long> dataItemIds, long userId, string role)
    {
        if (!await _context.Users.AnyAsync(u => u.Id == userId))
            throw new KeyNotFoundException("User not found.");

        if (dataItemIds == null || dataItemIds.Count == 0)
            throw new InvalidOperationException("No data items provided.");

        // Get the dataset from the first data item
        var firstItem = await _context.DataItems
            .Include(di => di.Dataset)
            .FirstOrDefaultAsync(di => dataItemIds.Contains(di.Id));
        if (firstItem == null)
            throw new KeyNotFoundException("Data items not found.");

        long datasetId = firstItem.DatasetId;
        long projectId = firstItem.Dataset.ProjectId;

        // Create or update DatasetAssignment (for context/tracking)
        var datasetAssignment = await _context.DatasetAssignments
            .FirstOrDefaultAsync(da => da.DatasetId == datasetId && da.UserId == userId);
        
        if (datasetAssignment == null)
        {
            _context.DatasetAssignments.Add(new DatasetAssignment
            {
                DatasetId = datasetId,
                UserId = userId,
                Role = role,
                AssignedAt = DateTime.UtcNow
            });
        }
        else
        {
            datasetAssignment.Role = role;
            datasetAssignment.AssignedAt = DateTime.UtcNow;
            _context.DatasetAssignments.Update(datasetAssignment);
        }

        // Assign individual data items
        foreach (var dataItemId in dataItemIds)
        {
            var dataItem = await _context.DataItems.FindAsync(dataItemId);
            if (dataItem == null) continue;

            // Update dataitem status to assigned
            dataItem.Status = "assigned";
            _context.DataItems.Update(dataItem);

            // Create or update DataItemAssignment
            var existingAssignment = await _context.DataItemAssignments
                .FirstOrDefaultAsync(da => da.DataItemId == dataItemId && da.UserId == userId);
            
            if (existingAssignment != null)
            {
                throw new InvalidOperationException($"Data item {dataItemId} has already been assigned to this user.");
            }

            _context.DataItemAssignments.Add(new DataItemAssignment
            {
                DataItemId = dataItemId,
                UserId = userId,
                Status = "assigned",
                AssignedAt = DateTime.UtcNow
            });
        }

        // Add user to ProjectMembers if not already a member
        var existingMember = await _context.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (existingMember == null)
        {
            _context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                RoleInProject = role,
                JoinedAt = DateTime.UtcNow
            });
        }
        else if (existingMember.RoleInProject != role)
        {
            existingMember.RoleInProject = role;
            _context.ProjectMembers.Update(existingMember);
        }

        await _context.SaveChangesAsync();

        // Check if all data items in dataset are now assigned
        await UpdateDatasetStatusAsync(datasetId);

        return true;
    }

    // Helper method to auto-update dataset status when all items are assigned
    private async Task UpdateDatasetStatusAsync(long datasetId)
    {
        var dataset = await _context.Datasets.FindAsync(datasetId);
        if (dataset == null) return;

        var totalItems = await _context.DataItems.CountAsync(di => di.DatasetId == datasetId);
        var assignedItems = await _context.DataItems.CountAsync(di => di.DatasetId == datasetId && di.Status == "assigned");

        // If all items are assigned, mark dataset as assigned
        if (totalItems > 0 && totalItems == assignedItems)
        {
            dataset.Status = "assigned";
            _context.Datasets.Update(dataset);
            await _context.SaveChangesAsync();
        }
    }
    #endregion

    #region Quality
    public async Task<List<QualityOverviewByProjectDto>> GetQualityOverviewByProjectAsync()
    {
        // Get all annotations with their reviews grouped by project
        var annotationsWithReviews = await _context.Annotations
            .Include(a => a.Reviews)
            .Include(a => a.DataItem).ThenInclude(di => di.Dataset).ThenInclude(d => d.Project)
            .ToListAsync();

        var qualityByProject = annotationsWithReviews
            .Where(a => a.DataItem != null && a.DataItem.Dataset != null)
            .GroupBy(a => a.DataItem.Dataset.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                ProjectName = g.First().DataItem?.Dataset?.Project?.Name ?? "Unknown",
                TotalAnnotations = g.Count(),
                Approved = g.Count(a => a.Reviews?.Any(r => r.Status == "approved") == true),
                Rejected = g.Count(a => a.Reviews?.Any(r => r.Status == "rejected") == true && a.Reviews?.Any(r => r.Status == "approved") != true)
            })
            .ToList();

        var result = qualityByProject.Select(qp => new QualityOverviewByProjectDto
        {
            ProjectId = qp.ProjectId,
            ProjectName = qp.ProjectName,
            TotalAnnotations = qp.TotalAnnotations,
            Approved = qp.Approved,
            Rejected = qp.Rejected
        })
        .OrderByDescending(x => x.ApprovalRate)
        .ToList();

        return result;
    }

    public async Task<List<QualityOverviewByDatasetDto>> GetQualityOverviewByDatasetAsync()
    {
        // Get all annotations with their reviews grouped by dataset
        var annotationsWithReviews = await _context.Annotations
            .Include(a => a.Reviews)
            .Include(a => a.DataItem).ThenInclude(di => di.Dataset).ThenInclude(d => d.Project)
            .ToListAsync();

        var qualityByDataset = annotationsWithReviews
            .Where(a => a.DataItem != null && a.DataItem.Dataset != null)
            .GroupBy(a => a.DataItem.DatasetId)
            .Select(g => new
            {
                DatasetId = g.Key,
                DatasetName = g.First().DataItem?.Dataset?.Name ?? "Unknown",
                ProjectId = g.First().DataItem?.Dataset?.ProjectId ?? 0,
                ProjectName = g.First().DataItem?.Dataset?.Project?.Name ?? "Unknown",
                TotalAnnotations = g.Count(),
                Approved = g.Count(a => a.Reviews?.Any(r => r.Status == "approved") == true),
                Rejected = g.Count(a => a.Reviews?.Any(r => r.Status == "rejected") == true && a.Reviews?.Any(r => r.Status == "approved") != true)
            })
            .ToList();

        var result = qualityByDataset.Select(qd => new QualityOverviewByDatasetDto
        {
            DatasetId = qd.DatasetId,
            DatasetName = qd.DatasetName,
            ProjectId = qd.ProjectId,
            ProjectName = qd.ProjectName,
            TotalAnnotations = qd.TotalAnnotations,
            Approved = qd.Approved,
            Rejected = qd.Rejected
        })
        .OrderByDescending(x => x.ApprovalRate)
        .ToList();

        return result;
    }

    public async Task<List<QualityOverviewByDataItemDto>> GetQualityOverviewByDataItemAsync()
    {
        // Get all reviews grouped by data item
        var reviewsWithDetails = await _context.Reviews
            .Include(r => r.Annotation).ThenInclude(a => a.DataItem).ThenInclude(di => di.Dataset)
            .ToListAsync();

        var qualityByDataItem = reviewsWithDetails
            .Where(r => r.Annotation != null && r.Annotation.DataItem != null)
            .GroupBy(r => r.Annotation.DataItemId)
            .Select(g => new
            {
                DataItemId = g.Key,
                DataItemContent = g.First().Annotation?.DataItem?.Content ?? "Unknown",
                DatasetId = g.First().Annotation?.DataItem?.DatasetId ?? 0,
                DatasetName = g.First().Annotation?.DataItem?.Dataset?.Name ?? "Unknown",
                TotalAnnotations = g.Count(), // Reviews count
                Approved = g.Count(r => r.Status == "approved"),
                Rejected = g.Count(r => r.Status == "rejected")
            })
            .ToList();

        var result = qualityByDataItem.Select(qdi => new QualityOverviewByDataItemDto
        {
            DataItemId = qdi.DataItemId,
            DataItemContent = qdi.DataItemContent,
            DatasetId = qdi.DatasetId,
            DatasetName = qdi.DatasetName,
            TotalAnnotations = qdi.TotalAnnotations,
            Approved = qdi.Approved,
            Rejected = qdi.Rejected
        })
        .OrderByDescending(x => x.ApprovalRate)
        .ToList();

        return result;
    }

    public async Task<List<QualityOverviewByAnnotatorDto>> GetQualityOverviewByAnnotatorAsync()
    {
        // Get all reviews grouped by annotator (whose annotations were reviewed)
        var reviewsWithDetails = await _context.Reviews
            .Include(r => r.Annotation).ThenInclude(a => a.User)
            .Include(r => r.Annotation).ThenInclude(a => a.DataItem).ThenInclude(di => di.Dataset)
            .ToListAsync();

        var qualityByAnnotator = reviewsWithDetails
            .Where(r => r.Annotation != null && r.Annotation.User != null)
            .GroupBy(r => new { r.Annotation.UserId, UserName = r.Annotation.User.Username })
            .Select(g => new
            {
                AnnotatorId = g.Key.UserId,
                AnnotatorName = g.Key.UserName,
                TotalAnnotations = g.Count(), // Reviews count
                Approved = g.Count(r => r.Status == "approved"),
                Rejected = g.Count(r => r.Status == "rejected"),
                ProjectCount = g.Select(r => r.Annotation?.DataItem?.Dataset?.ProjectId ?? 0).Distinct().Count()
            })
            .ToList();

        var result = qualityByAnnotator.Select(qa => new QualityOverviewByAnnotatorDto
        {
            AnnotatorId = qa.AnnotatorId,
            AnnotatorName = qa.AnnotatorName,
            TotalAnnotations = qa.TotalAnnotations,
            Approved = qa.Approved,
            Rejected = qa.Rejected,
            ProjectsWorkedOn = qa.ProjectCount
        })
        .OrderByDescending(x => x.ApprovalRate)
        .ToList();

        return result;
    }
    #endregion

    #region Export
    public async Task<ExportJobDto> CreateExportJobAsync(long projectId, ExportRequestDto request, long createdByUserId)
    {
        if (!await _context.Projects.AnyAsync(p => p.Id == projectId)) throw new KeyNotFoundException("Project not found.");

        var job = new ExportJob
        {
            ProjectId = projectId,
            DatasetId = request.DatasetId,
            ExportFormat = request.ExportFormat,
            CreatedBy = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ExportJobs.Add(job);
        await _context.SaveChangesAsync();

        // Normally, here a background service would populate ExportItems table asynchronously
        // For simplicity we just return the job immediately

        return new ExportJobDto
        {
            Id = job.Id,
            ProjectId = job.ProjectId,
            DatasetId = job.DatasetId,
            ExportFormat = job.ExportFormat,
            CreatedBy = job.CreatedBy,
            CreatedAt = job.CreatedAt
        };
    }

    public async Task<PagedResult<ExportJobDto>> GetExportJobsAsync(long projectId, int pageNumber = 1, int pageSize = 20)
    {
        var query = _context.ExportJobs.Where(e => e.ProjectId == projectId);
        var total = await query.CountAsync();
        var data = await query.OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExportJobDto
            {
                Id = e.Id,
                ProjectId = e.ProjectId,
                DatasetId = e.DatasetId,
                ExportFormat = e.ExportFormat,
                CreatedBy = e.CreatedBy,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();
        return new PagedResult<ExportJobDto> { Items = data, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }
    #endregion

    #region User Management
    public async Task<PagedResult<DataLabeling.Domain.DTOs.Manager.UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? role = null, string? status = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => u.Status == status);
        }

        var total = await query.CountAsync();
        var data = await query.OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new DataLabeling.Domain.DTOs.Manager.UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                Status = u.Status,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<DataLabeling.Domain.DTOs.Manager.UserDto> { Items = data, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<DataLabeling.Domain.DTOs.Manager.UserDto?> GetUserByIdAsync(long id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        return new DataLabeling.Domain.DTOs.Manager.UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        };
    }
    #endregion
}


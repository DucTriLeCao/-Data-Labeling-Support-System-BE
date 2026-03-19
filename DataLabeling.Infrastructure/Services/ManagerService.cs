using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Common;
using DataLabeling.Domain.DTOs.Manager;
using DataLabeling.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataLabeling.Infrastructure.Services;

public class ManagerService : IManagerService
{
    private readonly DataLabelingDBContext _context;

    public ManagerService(DataLabelingDBContext context)
    {
        _context = context;
    }

    #region Projects
    public async Task<PagedResult<ProjectDto>> GetProjectsAsync(int pageNumber, int pageSize, string? status = null)
    {
        var query = _context.Projects.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
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
    #endregion

    #region Datasets
    public async Task<PagedResult<DatasetDto>> GetDatasetsAsync(long projectId, int pageNumber, int pageSize, string? status = null)
    {
        var query = _context.Datasets.Where(d => d.ProjectId == projectId).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(d => d.Status == status);

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

    public async Task<PagedResult<DataItemDto>> GetDataItemsAsync(long datasetId, int pageNumber, int pageSize, string? status = null)
    {
        var query = _context.DataItems.Where(di => di.DatasetId == datasetId).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(di => di.Status == status);

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
        
        // ensure no child labels depend on it
        if (await _context.Labels.AnyAsync(child => child.ParentId == id))
            throw new InvalidOperationException("Cannot delete label because it has sub-labels.");

        _context.Labels.Remove(l);
        await _context.SaveChangesAsync();
        return true;
    }
    #endregion

    #region Assignment & Progress
    public async Task<bool> AssignDatasetAsync(long datasetId, AssignDatasetDto request)
    {
        if (!await _context.Datasets.AnyAsync(d => d.Id == datasetId)) throw new KeyNotFoundException("Dataset not found.");
        if (!await _context.Users.AnyAsync(u => u.Id == request.UserId)) throw new KeyNotFoundException("User not found.");

        var existing = await _context.DatasetAssignments.FirstOrDefaultAsync(da => da.DatasetId == datasetId && da.UserId == request.UserId);
        if (existing != null)
        {
            existing.Role = request.Role;
            existing.AssignedAt = DateTime.UtcNow;
            _context.DatasetAssignments.Update(existing);
        }
        else
        {
            _context.DatasetAssignments.Add(new DatasetAssignment
            {
                DatasetId = datasetId,
                UserId = request.UserId,
                Role = request.Role,
                AssignedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();
        return true;
    }

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
    #endregion

    #region Quality
    public async Task<QualityOverviewDto> GetQualityOverviewAsync(long projectId)
    {
        if (!await _context.Projects.AnyAsync(p => p.Id == projectId)) throw new KeyNotFoundException("Project not found.");

        // Reviews are linked to Annotations -> DataItems -> Datasets -> Project
        var annotationsInProject = _context.Annotations
            .Include(a => a.DataItem).ThenInclude(di => di.Dataset)
            .Where(a => a.DataItem.Dataset.ProjectId == projectId);

        var reviewsInProject = _context.Reviews
            .Include(r => r.Annotation).ThenInclude(a => a.DataItem).ThenInclude(di => di.Dataset)
            .Where(r => r.Annotation.DataItem.Dataset.ProjectId == projectId);

        int totalAnnotations = await annotationsInProject.CountAsync();
        int approved = await reviewsInProject.CountAsync(r => r.Status == "approved");
        int rejected = await reviewsInProject.CountAsync(r => r.Status == "rejected");

        return new QualityOverviewDto
        {
            ProjectId = projectId,
            TotalAnnotations = totalAnnotations,
            ApprovedCount = approved,
            RejectedCount = rejected
        };
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

    public async Task<IEnumerable<ExportJobDto>> GetExportJobsAsync(long projectId)
    {
        return await _context.ExportJobs
            .Where(e => e.ProjectId == projectId)
            .OrderByDescending(e => e.CreatedAt)
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
    }
    #endregion
}

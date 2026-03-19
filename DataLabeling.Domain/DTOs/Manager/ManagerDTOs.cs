using System.ComponentModel.DataAnnotations;
using DataLabeling.Domain.Models;

namespace DataLabeling.Domain.DTOs.Manager;

// Project DTOs
public class ProjectDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

public class CreateProjectDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateProjectDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
}

// Dataset DTOs
public class DatasetDto
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

public class CreateDatasetDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateDatasetDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
}

// DataItem DTOs
public class DataItemDto
{
    public long Id { get; set; }
    public long DatasetId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

// Label DTOs
public class LabelDto
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? ParentId { get; set; }
}

public class CreateLabelDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public long? ParentId { get; set; }
}

public class UpdateLabelDto
{
    public string? Name { get; set; }
    public long? ParentId { get; set; }
}

// Assignment DTOs
public class AssignDatasetDto
{
    [Required]
    public long UserId { get; set; }
    [Required]
    public string Role { get; set; } = string.Empty; // "Annotator" or "Reviewer"
}

public class DatasetProgressDto
{
    public long DatasetId { get; set; }
    public int TotalDataItems { get; set; }
    public int AnnotatedItems { get; set; }
    public int ReviewedItems { get; set; }
    public int PendingItems { get; set; }
}

// Quality Overview DTO
public class QualityOverviewDto
{
    public long ProjectId { get; set; }
    public int TotalAnnotations { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public double ApprovalRate => TotalAnnotations > 0 ? (double)ApprovedCount / TotalAnnotations * 100 : 0;
}

// Export DTO
public class ExportRequestDto
{
    public long? DatasetId { get; set; }
    [Required]
    public string ExportFormat { get; set; } = "JSON";
}

public class ExportJobDto
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public long? DatasetId { get; set; }
    public string ExportFormat { get; set; } = string.Empty;
    public long CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
}

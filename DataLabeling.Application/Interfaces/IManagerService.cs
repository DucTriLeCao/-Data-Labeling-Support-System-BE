using DataLabeling.Domain.DTOs.Common;
using DataLabeling.Domain.DTOs.Manager;

namespace DataLabeling.Application.Interfaces;

public interface IManagerService
{
    // Project Management
    Task<PagedResult<ProjectDto>> GetProjectsAsync(int pageNumber, int pageSize, string? status = null);
    Task<ProjectDto?> GetProjectByIdAsync(long id);
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto request);
    Task<ProjectDto> UpdateProjectAsync(long id, UpdateProjectDto request);

    // Dataset Management
    Task<PagedResult<DatasetDto>> GetDatasetsAsync(long projectId, int pageNumber, int pageSize, string? status = null);
    Task<DatasetDto> CreateDatasetAsync(long projectId, CreateDatasetDto request);
    Task<DatasetDto> UpdateDatasetAsync(long id, UpdateDatasetDto request);

    // Data Item Management
    Task<PagedResult<DataItemDto>> GetDataItemsAsync(long datasetId, int pageNumber, int pageSize, string? status = null);
    Task<DataItemDto> CreateDataItemAsync(long datasetId, Stream fileStream, string fileName);

    // Label Configuration
    Task<PagedResult<LabelDto>> GetLabelsAsync(long projectId, int pageNumber, int pageSize);
    Task<LabelDto> CreateLabelAsync(long projectId, CreateLabelDto request);
    Task<LabelDto> UpdateLabelAsync(long id, UpdateLabelDto request);
    Task<bool> DeleteLabelAsync(long id);

    // Task Assignment & Tracking
    Task<bool> AssignDatasetAsync(long datasetId, AssignDatasetDto request);
    Task<DatasetProgressDto> GetDatasetProgressAsync(long datasetId);

    // Quality Overview
    Task<QualityOverviewDto> GetQualityOverviewAsync(long projectId);

    // Export Data
    Task<ExportJobDto> CreateExportJobAsync(long projectId, ExportRequestDto request, long createdByUserId);
    Task<IEnumerable<ExportJobDto>> GetExportJobsAsync(long projectId);
}

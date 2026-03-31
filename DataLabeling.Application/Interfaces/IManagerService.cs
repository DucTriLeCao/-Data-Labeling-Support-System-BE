using DataLabeling.Domain.DTOs.Common;
using DataLabeling.Domain.DTOs.Manager;
using DataLabeling.Domain.DTOs.Admin.UserMgmt;

namespace DataLabeling.Application.Interfaces;

public interface IManagerService
{
    // Project Management
    Task<PagedResult<ProjectDto>> GetProjectsAsync(int pageNumber, int pageSize, string? status = null, string? searchTerm = null);
    Task<ProjectDto?> GetProjectByIdAsync(long id);
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto request);
    Task<ProjectDto> UpdateProjectAsync(long id, UpdateProjectDto request);
    Task<bool> DeleteProjectAsync(long id);

    // Dataset Management
    Task<PagedResult<DatasetDto>> GetDatasetsAsync(long projectId, int pageNumber, int pageSize, string? status = null, string? searchTerm = null);
    Task<DatasetDto?> GetDatasetByIdAsync(long id);
    Task<DatasetDto> CreateDatasetAsync(long projectId, CreateDatasetDto request);
    Task<DatasetDto> UpdateDatasetAsync(long id, UpdateDatasetDto request);
    Task<bool> DeleteDatasetAsync(long id);

    // Data Item Management
    Task<PagedResult<DataItemDto>> GetDataItemsAsync(long datasetId, int pageNumber, int pageSize, string? status = null, string? searchTerm = null);
    Task<DataItemDto?> GetDataItemByIdAsync(long id);
    Task<DataItemDto> CreateDataItemAsync(long datasetId, Stream fileStream, string fileName);
    Task<bool> DeleteDataItemAsync(long id);

    // Label Configuration
    Task<PagedResult<LabelDto>> GetLabelsAsync(long projectId, int pageNumber, int pageSize);
    Task<LabelDto?> GetLabelByIdAsync(long id);
    Task<LabelDto> CreateLabelAsync(long projectId, CreateLabelDto request);
    Task<LabelDto> UpdateLabelAsync(long id, UpdateLabelDto request);
    Task<bool> DeleteLabelAsync(long id);
    Task<BulkDeleteResultDto> BulkDeleteLabelsAsync(List<long> labelIds);

    // Bulk Delete
    Task<BulkDeleteResultDto> BulkDeleteProjectsAsync(List<long> projectIds);
    Task<BulkDeleteResultDto> BulkDeleteDatasetsAsync(List<long> datasetIds);
    Task<BulkDeleteResultDto> BulkDeleteDataItemsAsync(List<long> dataItemIds);

    // Task Assignment & Tracking
    Task<bool> AssignDatasetAsync(long datasetId, AssignDatasetDto request);
    Task<DatasetProgressDto> GetDatasetProgressAsync(long datasetId);
    Task<List<DatasetAssignmentDto>> GetDatasetAssignmentsAsync(long datasetId);

    // Quality Overview
    Task<QualityOverviewDto> GetQualityOverviewAsync(long projectId);

    // Export Data
    Task<ExportJobDto> CreateExportJobAsync(long projectId, ExportRequestDto request, long createdByUserId);
    Task<PagedResult<ExportJobDto>> GetExportJobsAsync(long projectId, int pageNumber = 1, int pageSize = 20);

    // User Management
    Task<PagedResult<DataLabeling.Domain.DTOs.Manager.UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? role = null, string? status = null);
    Task<DataLabeling.Domain.DTOs.Manager.UserDto?> GetUserByIdAsync(long id);
}

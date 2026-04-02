using DataLabeling.Domain.DTOs.Admin.ActivityLog;
using DataLabeling.Domain.DTOs.Admin.UserMgmt;
using DataLabeling.Domain.DTOs.Common;

namespace DataLabeling.Application.Interfaces;

public interface IAdminService
{
    // User Management
    Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? role = null, string? status = null);
    Task<UserDto?> GetUserByIdAsync(long id);
    Task<UserDto> CreateUserAsync(CreateUserDto request);
    Task<UserDto> UpdateUserAsync(long id, UpdateUserDto request);
    Task<bool> DeactivateUserAsync(long id);
    Task<BulkDeleteResultDto> BulkDeactivateUsersAsync(List<long> userIds);

    // Activity Logging
    Task<PagedResult<ActivityLogDto>> GetActivityLogsAsync(ActivityLogQueryDto query);
    Task LogActivityAsync(long userId, string action, string entityType, long? entityId = null);
}

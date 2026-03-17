using DataLabeling.Domain.DTOs.Admin.ActivityLog;
using DataLabeling.Domain.DTOs.Common;

namespace DataLabeling.Application.Interfaces;

public interface IActivityLogService
{
    Task<PagedResult<ActivityLogDto>> GetActivityLogsAsync(ActivityLogQueryDto query);
    Task LogActivityAsync(long userId, string action, string entityType, long? entityId = null);
}

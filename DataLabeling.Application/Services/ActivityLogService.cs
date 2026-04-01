using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Admin.ActivityLog;
using DataLabeling.Domain.DTOs.Common;
using DataLabeling.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataLabeling.Application.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly DataLabelingDBContext _context;

    public ActivityLogService(DataLabelingDBContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ActivityLogDto>> GetActivityLogsAsync(ActivityLogQueryDto query)
    {
        var dbQuery = _context.ActivityLogs.Include(a => a.User).AsQueryable();

        if (query.UserId.HasValue)
        {
            dbQuery = dbQuery.Where(a => a.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            dbQuery = dbQuery.Where(a => a.Action.Contains(query.Action));
        }

        var totalCount = await dbQuery.CountAsync();

        var logs = await dbQuery
            .OrderByDescending(a => a.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new ActivityLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Username = a.User.Username,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<ActivityLogDto>
        {
            Items = logs,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task LogActivityAsync(long userId, string action, string entityType, long? entityId = null)
    {
        var log = new ActivityLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}


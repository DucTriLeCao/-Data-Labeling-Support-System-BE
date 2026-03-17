using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Admin.ActivityLog;
using DataLabeling.Domain.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataLabeling.API.Controllers.Admin;

[ApiController]
[Route("api/admin/activity-logs")]
[Authorize(Roles = UserRole.Admin)]
public class AdminActivityLogsController : ControllerBase
{
    private readonly IActivityLogService _activityLogService;

    public AdminActivityLogsController(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActivityLogs([FromQuery] ActivityLogQueryDto query)
    {
        var result = await _activityLogService.GetActivityLogsAsync(query);
        return Ok(result);
    }
}

using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Admin.ActivityLog;
using DataLabeling.Domain.DTOs.Admin.UserMgmt;
using DataLabeling.Domain.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataLabeling.API.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = UserRole.Admin)]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    #region User Management

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? role = null, [FromQuery] string? status = null)
    {
        var result = await _adminService.GetUsersAsync(pageNumber, pageSize, role, status);
        return Ok(result);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById([FromRoute] long id)
    {
        var user = await _adminService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found.");
        }
        return Ok(user);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
    {
        try
        {
            var user = await _adminService.CreateUserAsync(request);
            
            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminIdClaim != null && long.TryParse(adminIdClaim.Value, out var adminId))
            {
                await _adminService.LogActivityAsync(adminId, "Created user", "User", user.Id);
            }

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser([FromRoute] long id, [FromBody] UpdateUserDto request)
    {
        try
        {
            var user = await _adminService.UpdateUserAsync(id, request);

            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminIdClaim != null && long.TryParse(adminIdClaim.Value, out var adminId))
            {
                await _adminService.LogActivityAsync(adminId, "Updated user", "User", user.Id);
            }

            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("users/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser([FromRoute] long id)
    {
        var success = await _adminService.DeactivateUserAsync(id);
        if (!success)
        {
            return NotFound($"User with ID {id} not found.");
        }

        var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (adminIdClaim != null && long.TryParse(adminIdClaim.Value, out var adminId))
        {
            await _adminService.LogActivityAsync(adminId, "Deactivated user", "User", id);
        }

        return NoContent();
    }

    [HttpDelete("users/bulk-deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeactivateUsers([FromBody] List<long> userIds)
    {
        if (userIds == null || userIds.Count == 0)
            return BadRequest(new { Message = "No user IDs provided" });

        var result = await _adminService.BulkDeactivateUsersAsync(userIds);

        var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (adminIdClaim != null && long.TryParse(adminIdClaim.Value, out var adminId))
        {
            await _adminService.LogActivityAsync(adminId, $"Bulk deactivated {result.SuccessfullyDeleted} users", "User", 0);
        }

        return Ok(result);
    }

    #endregion

    #region Activity Logging

    [HttpGet("activity-logs")]
    public async Task<IActionResult> GetActivityLogs([FromQuery] ActivityLogQueryDto query)
    {
        var result = await _adminService.GetActivityLogsAsync(query);
        return Ok(result);
    }

    #endregion
}

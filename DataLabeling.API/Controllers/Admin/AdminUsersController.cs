using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Admin.UserMgmt;
using DataLabeling.Domain.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataLabeling.API.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = UserRole.Admin)]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;
    private readonly IActivityLogService _activityLogService;

    public AdminUsersController(IAdminUserService adminUserService, IActivityLogService activityLogService)
    {
        _adminUserService = adminUserService;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? role = null, [FromQuery] string? status = null)
    {
        var result = await _adminUserService.GetUsersAsync(pageNumber, pageSize, role, status);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById([FromRoute] long id)
    {
        var user = await _adminUserService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found.");
        }
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
    {
        try
        {
            var user = await _adminUserService.CreateUserAsync(request);
            
            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminIdClaim != null && long.TryParse(adminIdClaim.Value, out var adminId))
            {
                await _activityLogService.LogActivityAsync(adminId, "Created user", "User", user.Id);
            }

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser([FromRoute] long id, [FromBody] UpdateUserDto request)
    {
        try
        {
            var user = await _adminUserService.UpdateUserAsync(id, request);

            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminIdClaim != null && long.TryParse(adminIdClaim.Value, out var adminId))
            {
                await _activityLogService.LogActivityAsync(adminId, "Updated user", "User", user.Id);
            }

            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser([FromRoute] long id)
    {
        var success = await _adminUserService.DeactivateUserAsync(id);
        if (!success)
        {
            return NotFound($"User with ID {id} not found.");
        }

        var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (adminIdClaim != null && long.TryParse(adminIdClaim.Value, out var adminId))
        {
            await _activityLogService.LogActivityAsync(adminId, "Deactivated user", "User", id);
        }

        return NoContent();
    }
}

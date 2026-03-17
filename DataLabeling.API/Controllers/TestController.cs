using DataLabeling.Domain.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataLabeling.API.Controllers;

/// <summary>
/// Test controller to demonstrate role-based authorization with JWT tokens
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// This endpoint requires authentication (any role)
    /// </summary>
    /// <returns>Authenticated user information</returns>
    /// <response code="200">Success - user is authenticated</response>
    /// <response code="401">Unauthorized - no valid token provided</response>
    [HttpGet("authenticated")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetAuthenticatedUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new
        {
            message = "You are authenticated!",
            userId,
            username,
            role
        });
    }

    /// <summary>
    /// This endpoint requires Admin role
    /// </summary>
    /// <returns>Admin access message</returns>
    /// <response code="200">Success - user has Admin role</response>
    /// <response code="401">Unauthorized - no valid token</response>
    /// <response code="403">Forbidden - user doesn't have Admin role</response>
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminOnly()
    {
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        _logger.LogInformation($"Admin accessed endpoint: {username}");

        return Ok(new
        {
            message = "Welcome Admin! You have access to admin functions.",
            username
        });
    }

    /// <summary>
    /// This endpoint requires Manager role
    /// </summary>
    /// <returns>Manager access message</returns>
    /// <response code="200">Success - user has Manager role</response>
    /// <response code="401">Unauthorized - no valid token</response>
    /// <response code="403">Forbidden - user doesn't have Manager role</response>
    [HttpGet("manager-only")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult ManagerOnly()
    {
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        _logger.LogInformation($"Manager accessed endpoint: {username}");

        return Ok(new
        {
            message = "Welcome Manager! You can manage projects and annotators.",
            username
        });
    }

    /// <summary>
    /// This endpoint requires Annotator role
    /// </summary>
    /// <returns>Annotator access message</returns>
    /// <response code="200">Success - user has Annotator role</response>
    /// <response code="401">Unauthorized - no valid token</response>
    /// <response code="403">Forbidden - user doesn't have Annotator role</response>
    [HttpGet("annotator-only")]
    [Authorize(Roles = "Annotator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AnnotatorOnly()
    {
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        _logger.LogInformation($"Annotator accessed endpoint: {username}");

        return Ok(new
        {
            message = "Welcome Annotator! You can perform annotation tasks.",
            username
        });
    }

    /// <summary>
    /// This endpoint requires Reviewer role
    /// </summary>
    /// <returns>Reviewer access message</returns>
    /// <response code="200">Success - user has Reviewer role</response>
    /// <response code="401">Unauthorized - no valid token</response>
    /// <response code="403">Forbidden - user doesn't have Reviewer role</response>
    [HttpGet("reviewer-only")]
    [Authorize(Roles = "Reviewer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult ReviewerOnly()
    {
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        _logger.LogInformation($"Reviewer accessed endpoint: {username}");

        return Ok(new
        {
            message = "Welcome Reviewer! You can review and validate annotations.",
            username
        });
    }

    /// <summary>
    /// This endpoint requires Admin or Manager roles
    /// </summary>
    /// <returns>Admin or Manager access message</returns>
    /// <response code="200">Success - user has Admin or Manager role</response>
    /// <response code="401">Unauthorized - no valid token</response>
    /// <response code="403">Forbidden - user doesn't have required roles</response>
    [HttpGet("admin-or-manager")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminOrManager()
    {
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        _logger.LogInformation($"{role} accessed management endpoint: {username}");

        return Ok(new
        {
            message = "Welcome Admin/Manager! You have management access.",
            username,
            role
        });
    }
}

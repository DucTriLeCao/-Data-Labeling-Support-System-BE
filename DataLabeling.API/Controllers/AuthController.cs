using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataLabeling.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// User login
    /// </summary>
    /// <param name="request">Login credentials (email and password)</param>
    /// <returns>JWT tokens and user information</returns>
    /// <response code="200">Login successful, returns access token and user info</response>
    /// <response code="400">Invalid credentials or request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Invalid request data"
                });
            }

            var result = await _authService.LoginAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"User logged in with email: {request.Email}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Login error: {ex.Message}");
            return StatusCode(500, new AuthResponse
            {
                IsSuccess = false,
                Message = "An error occurred during login"
            });
        }
    }

    /// <summary>
    /// User registration
    /// </summary>
    /// <param name="request">Registration data (username, email, password, and optional role)</param>
    /// <returns>JWT tokens and user information</returns>
    /// <response code="200">Registration successful</response>
    /// <response code="400">Invalid data or user already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Invalid request data"
                });
            }

            var result = await _authService.RegisterAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"New user {request.Username} registered successfully");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Registration error: {ex.Message}");
            return StatusCode(500, new AuthResponse
            {
                IsSuccess = false,
                Message = "An error occurred during registration"
            });
        }
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    /// <param name="refreshToken">Refresh token from login/register response</param>
    /// <returns>New access token</returns>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="400">Invalid refresh token</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Refresh token is required"
                });
            }

            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Refresh token error: {ex.Message}");
            return StatusCode(500, new AuthResponse
            {
                IsSuccess = false,
                Message = "An error occurred during token refresh"
            });
        }
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    /// <param name="request">Email or username to reset password</param>
    /// <returns>Forgot password response</returns>
    /// <response code="200">Password reset request processed</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.EmailOrUsername))
            {
                return BadRequest(new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Email or username is required"
                });
            }

            var result = await _authService.ForgotPasswordAsync(request.EmailOrUsername);

            _logger.LogInformation($"Password reset requested for {request.EmailOrUsername}");
            // Always return success for security (don't reveal if user exists)
            return Ok(new AuthResponse
            {
                IsSuccess = true,
                Message = "If an account exists, a password reset email will be sent"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Forgot password error: {ex.Message}");
            return StatusCode(500, new AuthResponse
            {
                IsSuccess = false,
                Message = "An error occurred"
            });
        }
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    /// <param name="request">Reset password request with token and new password</param>
    /// <returns>Reset password response</returns>
    /// <response code="200">Password reset successful</response>
    /// <response code="400">Invalid request or token</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Invalid request data"
                });
            }

            if (string.IsNullOrWhiteSpace(request.ResetToken))
            {
                return BadRequest(new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Reset token is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return BadRequest(new AuthResponse
                {
                    IsSuccess = false,
                    Message = "New password and confirm password are required"
                });
            }

            var result = await _authService.ResetPasswordAsync(request.UserId, request.ResetToken, request.NewPassword, request.ConfirmPassword);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"Password reset successfully for user {request.UserId}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Reset password error: {ex.Message}");
            return StatusCode(500, new AuthResponse
            {
                IsSuccess = false,
                Message = "An error occurred during password reset"
            });
        }
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    /// <param name="request">Change password request with current and new password</param>
    /// <returns>Change password response</returns>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Invalid request or incorrect current password</response>
    /// <response code="401">Unauthorized - user not authenticated</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Invalid request data"
                });
            }

            // Get user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new AuthResponse
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _authService.ChangePasswordAsync(userId, request);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation($"Password changed successfully for user {userId}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Change password error: {ex.Message}");
            return StatusCode(500, new AuthResponse
            {
                IsSuccess = false,
                Message = "An error occurred during password change"
            });
        }
    }
}

/// <summary>
/// Refresh token request model
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

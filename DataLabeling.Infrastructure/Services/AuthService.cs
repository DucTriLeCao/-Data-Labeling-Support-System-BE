using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Auth;
using DataLabeling.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataLabeling.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly DataLabelingDBContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;
    private readonly IPasswordResetService _passwordResetService;

    public AuthService(
        DataLabelingDBContext dbContext,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider,
        IPasswordResetService passwordResetService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        _passwordResetService = passwordResetService ?? throw new ArgumentNullException(nameof(passwordResetService));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Email and password are required"
                };
            }

            // Allow login with email (also checks username for backward compatibility)
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => 
                u.Email == request.Email || u.Username == request.Email);

            if (user == null)
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Invalid username or password"
                };
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Invalid username or password"
                };
            }

            // Check if user is active
            if (user.Status != "Active")
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "User account is inactive"
                };
            }

            // Generate tokens
            var accessToken = _tokenProvider.GenerateAccessToken(user.Id, user.Username, user.Role);
            var refreshToken = _tokenProvider.GenerateRefreshToken();

            return new AuthResponse
            {
                IsSuccess = true,
                Message = "Login successful",
                Data = new AuthTokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = 15 * 60, // 15 minutes in seconds
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role,
                        Status = user.Status
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = $"An error occurred during login: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Username, email, and password are required"
                };
            }

            if (request.Password.Length < 6)
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Password must be at least 6 characters long"
                };
            }

            // Check if username already exists
            if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username))
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Username already exists"
                };
            }

            // Check if email already exists
            if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Email already exists"
                };
            }

            // Validate role (case-insensitive)
            var role = request.Role;
            if (!UserRole.AllRoles.Contains(role))
            {
                // Try case-insensitive match
                var matchedRole = UserRole.AllRoles.FirstOrDefault(r => 
                    r.Equals(role, StringComparison.OrdinalIgnoreCase));
                
                role = matchedRole ?? UserRole.Annotator;
            }
            // Normalize to proper case
            role = UserRole.AllRoles.FirstOrDefault(r => 
                r.Equals(role, StringComparison.OrdinalIgnoreCase)) ?? role;

            // Create new user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Role = role,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Generate tokens
            var accessToken = _tokenProvider.GenerateAccessToken(user.Id, user.Username, user.Role);
            var refreshToken = _tokenProvider.GenerateRefreshToken();

            return new AuthResponse
            {
                IsSuccess = true,
                Message = "Registration successful",
                Data = new AuthTokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = 15 * 60,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role,
                        Status = user.Status
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = $"An error occurred during registration: {ex.Message}"
            };
        }
    }

    public Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Task.FromResult(new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Refresh token is required"
                });
            }

            // In a production environment, you would validate the refresh token against a stored token in the database
            // For now, we'll just generate a new access token
            // This is a simplified implementation

            return Task.FromResult(new AuthResponse
            {
                IsSuccess = false,
                Message = "Refresh token feature requires additional implementation"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new AuthResponse
            {
                IsSuccess = false,
                Message = $"An error occurred: {ex.Message}"
            });
        }
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        // Allow login with username or email
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => 
            u.Username == username || u.Email == username);

        if (user == null || user.Status != "Active")
            return false;

        return _passwordHasher.VerifyPassword(password, user.PasswordHash);
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return null;

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status
        };
    }

    public async Task<AuthResponse> ForgotPasswordAsync(string emailOrUsername)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(emailOrUsername))
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Email or username is required"
                };
            }

            var (success, token, message) = await _passwordResetService.GeneratePasswordResetTokenAsync(emailOrUsername);

            return new AuthResponse
            {
                IsSuccess = success,
                Message = message,
                Data = success ? new AuthTokenResponse { AccessToken = token } : null
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponse> ResetPasswordAsync(long userId, string resetToken, string newPassword, string confirmPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resetToken) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Reset token, new password, and confirm password are required"
                };
            }

            if (newPassword != confirmPassword)
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Passwords do not match"
                };
            }

            var (success, message) = await _passwordResetService.ResetPasswordAsync(userId, resetToken, newPassword);

            return new AuthResponse
            {
                IsSuccess = success,
                Message = message
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponse> ChangePasswordAsync(long userId, ChangePasswordRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CurrentPassword) || 
                string.IsNullOrWhiteSpace(request.NewPassword) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "Current password, new password, and confirm password are required"
                };
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = "New passwords do not match"
                };
            }

            var (success, message) = await _passwordResetService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

            return new AuthResponse
            {
                IsSuccess = success,
                Message = message
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }
}


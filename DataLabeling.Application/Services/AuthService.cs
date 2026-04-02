using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Auth;
using DataLabeling.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataLabeling.Application.Services;

public class AuthService : IAuthService
{
    private readonly DataLabelingDBContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;
    public AuthService(
        DataLabelingDBContext dbContext,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
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



    /// <summary>
    /// Normalizes role to proper casing by matching against valid roles in UserRole class.
    /// </summary>
    private string NormalizeRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return UserRole.Annotator;

        var normalizedRole = UserRole.AllRoles.FirstOrDefault(r => 
            r.Equals(role, StringComparison.OrdinalIgnoreCase));
        
        return normalizedRole ?? UserRole.Annotator;
    }

    /// <summary>
    /// Normalizes status to proper casing by matching against valid statuses in UserStatus class.
    /// </summary>
    private string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return UserStatus.Active;

        var normalizedStatus = UserStatus.AllStatuses.FirstOrDefault(s => 
            s.Equals(status, StringComparison.OrdinalIgnoreCase));
        
        return normalizedStatus ?? UserStatus.Active;
    }
}



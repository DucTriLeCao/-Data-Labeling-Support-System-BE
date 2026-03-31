using DataLabeling.Application.Interfaces;
using DataLabeling.Domain.DTOs.Admin.UserMgmt;
using DataLabeling.Domain.DTOs.Common;
using DataLabeling.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataLabeling.Infrastructure.Services;

public class AdminUserService : IAdminUserService
{
    private readonly DataLabelingDBContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AdminUserService(DataLabelingDBContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? role = null, string? status = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => u.Status == status);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                Status = u.Status,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<UserDto>
        {
            Items = users,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(long id)
    {
        var user = await _context.Users
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                Status = u.Status,
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto request)
    {
        // Check if username or email already exists
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new InvalidOperationException("Username already exists.");
        }

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = request.Role,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserDto> UpdateUserAsync(long id, UpdateUserDto request)
    {
        var user = await _context.Users.FindAsync(id);
        
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {id} not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            user.Role = request.Role;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            user.Status = request.Status;
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = _passwordHasher.HashPassword(request.Password);
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> DeactivateUserAsync(long id)
    {
        var user = await _context.Users.FindAsync(id);
        
        if (user == null)
        {
            return false;
        }

        user.Status = "Inactive";
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<BulkDeleteResultDto> BulkDeactivateUsersAsync(List<long> userIds)
    {
        var result = new BulkDeleteResultDto { TotalRequested = userIds.Count };

        foreach (var userId in userIds)
        {
            try
            {
                if (await DeactivateUserAsync(userId))
                {
                    result.SuccessfullyDeleted++;
                }
                else
                {
                    result.Failed++;
                    result.Errors.Add(new BulkDeleteErrorDto { Id = userId, Reason = "User not found" });
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add(new BulkDeleteErrorDto { Id = userId, Reason = ex.Message });
            }
        }

        return result;
    }
}

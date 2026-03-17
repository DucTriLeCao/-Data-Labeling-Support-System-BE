using DataLabeling.Domain.DTOs.Admin.UserMgmt;
using DataLabeling.Domain.DTOs.Common;

namespace DataLabeling.Application.Interfaces;

public interface IAdminUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(int pageNumber, int pageSize, string? role = null, string? status = null);
    Task<UserDto?> GetUserByIdAsync(long id);
    Task<UserDto> CreateUserAsync(CreateUserDto request);
    Task<UserDto> UpdateUserAsync(long id, UpdateUserDto request);
    Task<bool> DeactivateUserAsync(long id);
}

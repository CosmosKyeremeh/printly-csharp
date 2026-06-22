using Printly.Core.DTOs.Requests;
using Printly.Core.DTOs.Responses;
using Printly.Core.Enums;

namespace Printly.Core.Interfaces;

public interface IUserService
{
    Task<List<UserResponse>> GetOrgUsersAsync(Guid orgId);
    Task<UserResponse> GetUserByIdAsync(Guid userId);
    Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task ChangeRoleAsync(Guid targetUserId, UserRole newRole, Guid orgId);
}

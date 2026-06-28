using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Core.DTOs.Requests;
using Printly.Core.Enums;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;

namespace Printly.Web.Controllers;

[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly CurrentUserService _currentUser;

    public UsersController(IUserService userService, CurrentUserService currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// GET /api/users
    /// Admin only — list all users in the org.
    /// </summary>
    [Authorize(Roles = "Admin,Superadmin,PlatformOwner")]
    [HttpGet]
    public Task<IActionResult> GetUsers()
        => ExecuteAsync(() => _userService.GetOrgUsersAsync(_currentUser.OrgId));

    /// <summary>
    /// GET /api/users/me
    /// Any authenticated user can get their own profile.
    /// "me" is a named route segment — it won't conflict with GET /api/users/{id}
    /// because ASP.NET Core matches literal segments before parameter segments.
    /// </summary>
    [HttpGet("me")]
    public Task<IActionResult> GetMe()
        => ExecuteAsync(() => _userService.GetUserByIdAsync(_currentUser.UserId));

    /// <summary>
    /// PUT /api/users/me
    /// Update the current user's own profile.
    /// </summary>
    [HttpPut("me")]
    public Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        => ExecuteAsync(() => _userService.UpdateProfileAsync(
            _currentUser.UserId, request));

    /// <summary>
    /// PATCH /api/users/{id}/role
    /// Admin promotes or demotes another user's role.
    /// </summary>
    [Authorize(Roles = "Admin,Superadmin,PlatformOwner")]
    [HttpPatch("{id:guid}/role")]
    public Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            return Task.FromResult<IActionResult>(
                BadRequest(new { error = $"Invalid role: {request.Role}" }));

        return ExecuteAsync(() => _userService.ChangeRoleAsync(
            id, role, _currentUser.OrgId));
    }
}

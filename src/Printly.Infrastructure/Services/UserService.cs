using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Printly.Core.DTOs.Requests;
using Printly.Core.DTOs.Responses;
using Printly.Core.Entities;
using Printly.Core.Enums;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Data;

namespace Printly.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly PrintlyDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public UserService(PrintlyDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<UserResponse>> GetOrgUsersAsync(Guid orgId)
    {
        var users = await _context.Users
            .Include(u => u.Organization)
            .Where(u => u.OrgId == orgId)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return users.Select(u => new UserResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email!,
            PhoneNumber = u.PhoneNumber,
            WhatsAppNumber = u.WhatsAppNumber,
            Role = u.Role.ToString(),
            OrgId = u.OrgId,
            OrgName = u.Organization?.Name,
            CreatedAt = u.CreatedAt
        }).ToList();
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("User not found.");

        return new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            WhatsAppNumber = user.WhatsAppNumber,
            Role = user.Role.ToString(),
            OrgId = user.OrgId,
            OrgName = user.Organization?.Name,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserResponse> UpdateProfileAsync(
        Guid userId, UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.WhatsAppNumber = request.WhatsAppNumber;

        await _userManager.UpdateAsync(user);

        return new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            WhatsAppNumber = user.WhatsAppNumber,
            Role = user.Role.ToString()
        };
    }

    public async Task ChangeRoleAsync(
        Guid targetUserId, UserRole newRole, Guid orgId)
    {
        var user = await _userManager.FindByIdAsync(targetUserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        if (user.OrgId != orgId)
            throw new InvalidOperationException("User does not belong to this org.");

        // Remove all current roles then assign the new one.
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, newRole.ToString());

        user.Role = newRole;
        await _userManager.UpdateAsync(user);
    }
}

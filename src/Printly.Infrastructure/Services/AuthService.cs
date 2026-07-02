using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Printly.Core.DTOs.Requests;
using Printly.Core.DTOs.Responses;
using Printly.Core.Entities;
using Printly.Core.Enums;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Data;

namespace Printly.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly PrintlyDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(
        UserManager<AppUser> userManager,
        PrintlyDbContext context,
        IConfiguration config)
    {
        _userManager = userManager;
        _context = context;
        _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var orgExists = await _context.Organizations.AnyAsync();

        Organization org;
        UserRole role;

        if (!orgExists)
        {
            if (string.IsNullOrWhiteSpace(request.OrgName))
                throw new InvalidOperationException(
                    "Organisation name is required to create the first account.");

            org = new Organization
            {
                Name = request.OrgName,
                JoinCode = GenerateJoinCode()
            };

            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            role = UserRole.Superadmin;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.JoinCode))
                throw new InvalidOperationException("A join code is required to register.");

            org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.JoinCode == request.JoinCode)
                ?? throw new InvalidOperationException("Invalid join code.");

            role = UserRole.Student;
        }

        var user = new AppUser
        {
            FullName = request.FullName,
            Email = request.Email,
            UserName = request.Email,
            PhoneNumber = request.PhoneNumber,
            WhatsAppNumber = request.WhatsAppNumber,
            OrgId = org.Id,
            Role = role
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }

        await _userManager.AddToRoleAsync(user, role.ToString());

        // Add custom claims to Identity so they appear in the cookie
        await _userManager.AddClaimsAsync(user, new[]
        {
            new Claim("orgId", org.Id.ToString()),
            new Claim("fullName", user.FullName),
            new Claim("isPlatformOwner", user.IsPlatformOwner.ToString().ToLower())
        });

        if (org.CreatedByUserId == Guid.Empty)
        {
            org.CreatedByUserId = user.Id;
            await _context.SaveChangesAsync();
        }

        var token = GenerateJwt(user, org.Id);
        return BuildAuthResponse(user, org.Name, token);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("Invalid email or password.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            throw new InvalidOperationException("Invalid email or password.");

        var orgName = string.Empty;
        var orgId = user.OrgId ?? Guid.Empty;

        if (user.OrgId.HasValue)
        {
            var org = await _context.Organizations.FindAsync(user.OrgId.Value);
            orgName = org?.Name ?? string.Empty;
        }

        // Ensure custom claims exist on the user — add if missing
        var existingClaims = await _userManager.GetClaimsAsync(user);

        if (!existingClaims.Any(c => c.Type == "orgId"))
            await _userManager.AddClaimAsync(user, new Claim("orgId", orgId.ToString()));

        if (!existingClaims.Any(c => c.Type == "fullName"))
            await _userManager.AddClaimAsync(user, new Claim("fullName", user.FullName));

        if (!existingClaims.Any(c => c.Type == "isPlatformOwner"))
            await _userManager.AddClaimAsync(user,
                new Claim("isPlatformOwner", user.IsPlatformOwner.ToString().ToLower()));

        var token = GenerateJwt(user, orgId);
        return BuildAuthResponse(user, orgName, token);
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return;
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        _ = token;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("User not found.");

        var result = await _userManager.ResetPasswordAsync(
            user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }
    }

    private string GenerateJwt(AppUser user, Guid orgId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("orgId", orgId.ToString()),
            new("fullName", user.FullName),
            new("isPlatformOwner", user.IsPlatformOwner.ToString().ToLower()),
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(
            int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60"));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static AuthResponse BuildAuthResponse(AppUser user, string orgName, string token)
    {
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                WhatsAppNumber = user.WhatsAppNumber,
                Role = user.Role.ToString(),
                OrgId = user.OrgId,
                OrgName = orgName,
                CreatedAt = user.CreatedAt
            }
        };
    }

    private static string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(
            Enumerable.Repeat(chars, 6)
                      .Select(s => s[random.Next(s.Length)])
                      .ToArray());
    }
}

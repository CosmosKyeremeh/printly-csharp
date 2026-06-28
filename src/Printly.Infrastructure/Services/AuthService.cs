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

/// <summary>
/// Implements IAuthService using ASP.NET Core Identity for user management
/// and a hand-rolled JWT generator for stateless authentication.
///
/// STATELESS AUTHENTICATION:
/// Traditional web apps store session data on the server ("this session ID
/// belongs to user X"). JWTs flip this — all user info is encoded IN the
/// token itself. The server never stores sessions. It just validates the
/// token's signature on every request. This scales much better.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly PrintlyDbContext _context;
    private readonly IConfiguration _config;

    // UserManager<AppUser> is provided by ASP.NET Core Identity.
    // It handles password hashing, user creation, role assignment, etc.
    // We never hash passwords ourselves — Identity handles that securely.
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
        // Check if an organization already exists in the database.
        // If none exists, this is the very first user — they become Superadmin
        // and their registration creates the org.
        var orgExists = await _context.Organizations.AnyAsync();

        Organization org;
        UserRole role;

        if (!orgExists)
        {
            // PATH 1: No org exists — create one.
            // The first user becomes Superadmin and generates the join code
            // for other students to use at signup.
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
            // PATH 2: Org exists — student must provide a valid join code.
            if (string.IsNullOrWhiteSpace(request.JoinCode))
                throw new InvalidOperationException("A join code is required to register.");

            org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.JoinCode == request.JoinCode)
                ?? throw new InvalidOperationException("Invalid join code.");

            role = UserRole.Student;
        }

        // Build the AppUser object.
        // We set UserName = Email because Identity requires a UserName,
        // and using the email keeps things simple and consistent.
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

        // CreateAsync handles password hashing and saves the user.
        // NEVER store plain-text passwords — Identity uses PBKDF2 hashing.
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            // Identity returns structured errors — collect them all.
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }

        // Assign the role in Identity's AspNetUserRoles table.
        await _userManager.AddToRoleAsync(user, role.ToString());

        // If this was the first user, update the org with their ID.
        if (org.CreatedByUserId == Guid.Empty)
        {
            org.CreatedByUserId = user.Id;
            await _context.SaveChangesAsync();
        }

        var token = GenerateJwt(user);
        return BuildAuthResponse(user, org.Name, token);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // FindByEmailAsync looks up the user in AspNetUsers by email.
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("Invalid email or password.");

        // CheckPasswordAsync compares the provided password against the
        // stored hash. Returns false if wrong — never throws.
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            throw new InvalidOperationException("Invalid email or password.");

        // Load the org name for the response
        var orgName = string.Empty;
        if (user.OrgId.HasValue)
        {
            var org = await _context.Organizations.FindAsync(user.OrgId.Value);
            orgName = org?.Name ?? string.Empty;
        }

        var token = GenerateJwt(user);
        return BuildAuthResponse(user, orgName, token);
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        // We do NOT throw if the user doesn't exist.
        // This is intentional — it prevents attackers from using this
        // endpoint to find out which emails are registered (enumeration attack).
        if (user is null) return;

        // Identity generates a secure time-limited token for password reset.
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // TODO: Send email with reset link via MailKit (Step 8)
        // For now we just generate the token.
        _ = token;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("User not found.");

        // ResetPasswordAsync validates the token and updates the hash.
        var result = await _userManager.ResetPasswordAsync(
            user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }
    }

    // -- Private Helpers ----------------------------------------------------

    /// <summary>
    /// Generates a JWT (JSON Web Token) containing the user's key claims.
    ///
    /// A JWT has three parts separated by dots:
    ///   Header.Payload.Signature
    ///
    /// The Payload contains our claims (userId, orgId, role).
    /// The Signature is an HMAC-SHA256 hash of the header+payload using
    /// our secret key — this proves the token hasn't been tampered with.
    ///
    /// Anyone can READ the payload (it's base64, not encrypted).
    /// But nobody can FORGE a valid signature without knowing the secret key.
    /// </summary>
    private string GenerateJwt(AppUser user)
    {
        var claims = new List<Claim>
        {
            // NameIdentifier is the standard claim type for a user's ID.
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Role, user.Role.ToString()),

            // Custom claims — we'll read these in CurrentUserService
            new("orgId", user.OrgId?.ToString() ?? string.Empty),
            new("fullName", user.FullName),
            new("isPlatformOwner", user.IsPlatformOwner.ToString().ToLower()),
        };

        // The signing key — must be at least 32 characters (256 bits).
        // Stored in appsettings.Development.json; never hardcoded.
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var expiry = DateTime.UtcNow.AddMinutes(
            int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60"));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: credentials);

        // Serialize the token to its compact string form: xxx.yyy.zzz
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static AuthResponse BuildAuthResponse(
        AppUser user, string orgName, string token)
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

    /// <summary>
    /// Generates a random 6-character alphanumeric join code.
    /// e.g. "X7K2PQ"
    /// </summary>
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

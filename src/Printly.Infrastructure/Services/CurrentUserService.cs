using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Printly.Core.Enums;

namespace Printly.Infrastructure.Services;

/// <summary>
/// Reads the current user identity from whichever auth scheme was used.
/// For API calls this comes from JWT claims.
/// For Razor Pages this comes from cookie claims (set by SignInManager).
/// Both carry the same claim types because we added them via UserManager.AddClaimsAsync.
/// </summary>
public class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return value is null ? Guid.Empty : Guid.Parse(value);
        }
    }

    public Guid OrgId
    {
        get
        {
            var value = User?.FindFirstValue("orgId");
            if (value is null || string.IsNullOrWhiteSpace(value))
                return Guid.Empty;
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public string Role =>
        User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsPlatformOwner =>
        User?.FindFirstValue("isPlatformOwner") == "true";

    public bool IsAdmin =>
        Role is "Admin" or "Superadmin" or "PlatformOwner" || IsPlatformOwner;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;
}

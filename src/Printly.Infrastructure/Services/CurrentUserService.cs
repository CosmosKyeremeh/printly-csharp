using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Printly.Infrastructure.Services;

/// <summary>
/// Reads the currently authenticated user's identity from the JWT claims.
/// Injected into every service that needs to know who is making the request.
///
/// IHttpContextAccessor gives us access to the current HTTP request context
/// from inside a service class (which normally has no knowledge of HTTP).
/// </summary>
public class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // The ClaimsPrincipal represents the logged-in user and their claims.
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
            return value is null ? Guid.Empty : Guid.Parse(value);
        }
    }

    public string Role =>
        User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsPlatformOwner =>
        User?.FindFirstValue("isPlatformOwner") == "true";

    public bool IsAdmin =>
        Role is "Admin" or "Superadmin" || IsPlatformOwner;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;
}

using Microsoft.AspNetCore.Mvc;
using Printly.Core.DTOs.Requests;
using Printly.Core.Interfaces;

namespace Printly.Web.Controllers;

/// <summary>
/// Handles registration, login, and password reset.
/// All endpoints are public (no [Authorize] attribute) because
/// users aren't logged in yet when they call these.
/// </summary>
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    // The DI container sees this constructor and automatically
    // provides the IAuthService implementation we registered in Program.cs.
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// POST /api/auth/register
    /// Body: { fullName, email, password, joinCode?, orgName? }
    ///
    /// [FromBody] tells ASP.NET Core to deserialize the JSON request
    /// body into a RegisterRequest object automatically.
    /// </summary>
    [HttpPost("register")]
    public Task<IActionResult> Register([FromBody] RegisterRequest request)
        => ExecuteAsync(() => _authService.RegisterAsync(request));

    /// <summary>
    /// POST /api/auth/login
    /// Body: { email, password }
    /// Returns: { token, expiresAt, user }
    /// </summary>
    [HttpPost("login")]
    public Task<IActionResult> Login([FromBody] LoginRequest request)
        => ExecuteAsync(() => _authService.LoginAsync(request));

    /// <summary>
    /// POST /api/auth/forgot-password
    /// Body: { email }
    /// Always returns 200 — even if the email doesn't exist (prevents enumeration).
    /// </summary>
    [HttpPost("forgot-password")]
    public Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        => ExecuteAsync(() => _authService.ForgotPasswordAsync(request.Email));

    /// <summary>
    /// POST /api/auth/reset-password
    /// Body: { email, token, newPassword }
    /// </summary>
    [HttpPost("reset-password")]
    public Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        => ExecuteAsync(() => _authService.ResetPasswordAsync(request));
}

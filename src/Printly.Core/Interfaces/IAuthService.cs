using Printly.Core.DTOs.Requests;
using Printly.Core.DTOs.Responses;

namespace Printly.Core.Interfaces;

/// <summary>
/// Handles all authentication and onboarding flows:
/// registration (with org creation or join), login, and password reset.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user. Two scenarios:
    ///   1. No org exists yet ? creates one, user becomes Superadmin
    ///   2. JoinCode provided ? validates it, user joins as Student
    /// Returns a JWT on success.
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Validates credentials and returns a JWT + user info.
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Sends a password reset email with a secure token link.
    /// </summary>
    Task ForgotPasswordAsync(string email);

    /// <summary>
    /// Validates the reset token and updates the password.
    /// </summary>
    Task ResetPasswordAsync(ResetPasswordRequest request);
}

namespace Printly.Core.DTOs.Requests;

/// <summary>
/// Sent by the client when registering a new account.
/// JoinCode is optional — if omitted and no org exists, a new org is created.
/// OrgName is only used in that org-creation path.
/// </summary>
public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? WhatsAppNumber { get; set; }

    // Student join path
    public string? JoinCode { get; set; }

    // Org creation path (first user only)
    public string? OrgName { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

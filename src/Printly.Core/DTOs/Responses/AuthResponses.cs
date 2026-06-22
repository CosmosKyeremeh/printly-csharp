namespace Printly.Core.DTOs.Responses;

/// <summary>
/// Returned after a successful login or registration.
/// The Token is a JWT the client stores and sends on every
/// subsequent request in the Authorization header.
/// </summary>
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserResponse User { get; set; } = null!;
}

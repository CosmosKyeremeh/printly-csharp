namespace Printly.Core.DTOs.Responses;

/// <summary>
/// Safe public representation of a user.
/// Never includes PasswordHash or any sensitive Identity fields.
/// </summary>
public class UserResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public Guid? OrgId { get; set; }
    public string? OrgName { get; set; }
    public DateTime CreatedAt { get; set; }
}

namespace Printly.Core.DTOs.Requests;

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? WhatsAppNumber { get; set; }
}

public class ChangeRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

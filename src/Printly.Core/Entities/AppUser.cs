using Microsoft.AspNetCore.Identity;
using Printly.Core.Enums;

namespace Printly.Core.Entities;

/// <summary>
/// Extends ASP.NET Core Identity's built-in IdentityUser.
/// IdentityUser already gives us: Id, Email, PasswordHash, UserName,
/// PhoneNumber, EmailConfirmed, LockoutEnabled, and more.
/// We add our own fields on top: OrgId, Role, WhatsApp, IsPlatformOwner.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    // Which organization this user belongs to.
    // Nullable because a PlatformOwner has no org — they oversee all of them.
    public Guid? OrgId { get; set; }
    public Organization? Organization { get; set; }

    public UserRole Role { get; set; } = UserRole.Student;

    // WhatsApp number may differ from the regular phone number.
    public string? WhatsAppNumber { get; set; }

    // Platform owners bypass all org scoping — they see everything.
    public bool IsPlatformOwner { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<FileRecord> Files { get; set; } = new List<FileRecord>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
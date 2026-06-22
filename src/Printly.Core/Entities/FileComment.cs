namespace Printly.Core.Entities;

/// <summary>
/// A comment thread on a specific file — between a student and an admin.
/// Used for clarifications: "did you want this in colour?" etc.
/// </summary>
public class FileComment : BaseEntity
{
    public string Body { get; set; } = string.Empty;

    // Foreign keys
    public Guid FileRecordId { get; set; }
    public FileRecord FileRecord { get; set; } = null!;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public Guid OrgId { get; set; }
    public Organization Organization { get; set; } = null!;
}
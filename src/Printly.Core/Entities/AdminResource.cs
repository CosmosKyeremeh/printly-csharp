namespace Printly.Core.Entities;

/// <summary>
/// A file uploaded by an admin for students to download —
/// templates, reference sheets, assignment briefs, etc.
/// </summary>
public class AdminResource : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    // Foreign keys
    public Guid OrgId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid UploadedByUserId { get; set; }
    public AppUser UploadedBy { get; set; } = null!;
}
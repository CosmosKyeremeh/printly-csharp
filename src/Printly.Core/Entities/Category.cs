namespace Printly.Core.Entities;

/// <summary>
/// Admin-defined assignment types, e.g. "Lab Report", "Group Project".
/// Students select a category when uploading files.
/// Optional deadline triggers an automatic reminder notification.
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Nullable — not every category has a deadline.
    public DateTime? Deadline { get; set; }

    // Foreign key — every category belongs to one org.
    public Guid OrgId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }
    public AppUser CreatedBy { get; set; } = null!;

    public ICollection<FileRecord> Files { get; set; } = new List<FileRecord>();
}
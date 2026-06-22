using Printly.Core.Enums;

namespace Printly.Core.Entities;

/// <summary>
/// Represents a single file uploaded by a student.
/// This is the central entity of Printly — everything else
/// (queue items, payments, comments) hangs off a FileRecord.
/// </summary>
public class FileRecord : BaseEntity
{
    public string OriginalFileName { get; set; } = string.Empty;

    // The path or blob name where the file is physically stored.
    public string StoragePath { get; set; } = string.Empty;

    // MIME type e.g. "application/pdf", "application/vnd.openxmlformats..."
    public string ContentType { get; set; } = string.Empty;

    // File size in bytes — used to display "2.4 MB" in the UI.
    public long FileSizeBytes { get; set; }

    // Number of pages — used to auto-calculate price (GHS 1 per page).
    // Nullable because we may not know this until the file is analysed.
    public int? PageCount { get; set; }

    // Calculated price in Ghana Cedis. Admin can override this.
    public decimal Price { get; set; } = 0;

    // When true, admin has manually set the price and it won't be recalculated.
    public bool PriceLocked { get; set; } = false;

    // Notes the student leaves for the admin ("print double-sided, 3 copies").
    public string? PrintingInstructions { get; set; }

    public FileStatus Status { get; set; } = FileStatus.Queued;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    // Soft delete — we never permanently delete rows, just set this timestamp.
    public DateTime? DeletedAt { get; set; }

    // Foreign keys
    public Guid OrgId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    // Navigation
    public PrintQueueItem? QueueItem { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<FileComment> Comments { get; set; } = new List<FileComment>();
}
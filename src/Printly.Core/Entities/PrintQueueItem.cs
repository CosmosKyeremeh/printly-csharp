using Printly.Core.Enums;

namespace Printly.Core.Entities;

/// <summary>
/// Tracks the lifecycle of a file through the print queue.
/// One-to-one with FileRecord — every uploaded file gets exactly one queue item.
/// Stores WHO changed the status and WHEN, for a full audit trail.
/// </summary>
public class PrintQueueItem : BaseEntity
{
    public FileStatus Status { get; set; } = FileStatus.Queued;

    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PrintingStartedAt { get; set; }
    public DateTime? PrintedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Which admin last touched this queue item.
    public Guid? ActionedByUserId { get; set; }
    public AppUser? ActionedBy { get; set; }

    // Foreign key back to the file this queue item tracks.
    public Guid FileRecordId { get; set; }
    public FileRecord FileRecord { get; set; } = null!;

    public Guid OrgId { get; set; }
    public Organization Organization { get; set; } = null!;
}
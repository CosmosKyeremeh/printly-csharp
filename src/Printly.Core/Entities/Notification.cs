using Printly.Core.Enums;

namespace Printly.Core.Entities;

/// <summary>
/// A broadcast message sent by an admin to all members of an org.
/// ReadByUserIds tracks which users have opened/dismissed it.
/// We store this as a List<Guid> (a PostgreSQL array) to avoid
/// needing a separate NotificationRead join table.
/// </summary>
public class Notification : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.General;

    // List of user IDs who have marked this notification as read.
    // Stored as a Postgres array column (uuid[]).
    public List<Guid> ReadByUserIds { get; set; } = new List<Guid>();

    // Foreign keys
    public Guid OrgId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }
    public AppUser CreatedBy { get; set; } = null!;
}
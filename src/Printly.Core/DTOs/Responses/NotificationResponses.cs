namespace Printly.Core.DTOs.Responses;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    // True if the requesting user's ID is in ReadByUserIds.
    // Computed in the service layer, not stored as a column.
    public bool IsRead { get; set; }

    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

using Printly.Core.Enums;

namespace Printly.Core.DTOs.Requests;

public class CreateNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.General;
}

public class MarkAsReadRequest
{
    public List<Guid> NotificationIds { get; set; } = new();
}

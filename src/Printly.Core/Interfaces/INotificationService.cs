using Printly.Core.DTOs.Requests;
using Printly.Core.DTOs.Responses;

namespace Printly.Core.Interfaces;

/// <summary>
/// Handles creating, broadcasting, and reading notifications.
/// Works alongside SignalR — after saving to the DB, the service
/// triggers a SignalR push so the bell updates in real time.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Get all notifications for an org, with a flag showing
    /// whether the requesting user has read each one.
    /// </summary>
    Task<List<NotificationResponse>> GetOrgNotificationsAsync(
        Guid orgId,
        Guid userId);

    /// <summary>
    /// Admin broadcasts a notification to all org members.
    /// Saves to DB and fires a SignalR event.
    /// </summary>
    Task<NotificationResponse> CreateNotificationAsync(
        CreateNotificationRequest request,
        Guid adminUserId,
        Guid orgId);

    /// <summary>
    /// Mark one or more notifications as read for a specific user.
    /// Appends the userId to the ReadByUserIds array on each notification.
    /// </summary>
    Task MarkAsReadAsync(List<Guid> notificationIds, Guid userId);

    /// <summary>
    /// Returns the count of unread notifications for the bell badge.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, Guid orgId);
}

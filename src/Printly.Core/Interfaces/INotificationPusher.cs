namespace Printly.Core.Interfaces;

/// <summary>
/// Abstracts SignalR push notifications so Infrastructure
/// never needs to reference the Web project directly.
/// Program.cs registers the real implementation (SignalRNotificationPusher)
/// which lives in Web and knows about NotificationHub.
/// </summary>
public interface INotificationPusher
{
    Task PushToOrgAsync(string orgId, string eventName, object payload);
}

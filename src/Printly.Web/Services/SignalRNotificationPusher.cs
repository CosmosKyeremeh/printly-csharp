using Microsoft.AspNetCore.SignalR;
using Printly.Core.Interfaces;
using Printly.Web.Hubs;

namespace Printly.Web.Services;

/// <summary>
/// Implements INotificationPusher using SignalR.
/// Lives in Printly.Web (the only project that knows about NotificationHub).
/// Registered in Program.cs and injected into NotificationService via the interface.
/// </summary>
public class SignalRNotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationPusher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PushToOrgAsync(string orgId, string eventName, object payload)
    {
        await _hubContext.Clients.Group(orgId).SendAsync(eventName, payload);
    }
}

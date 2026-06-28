using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Printly.Infrastructure.Services;

namespace Printly.Web.Hubs;

/// <summary>
/// SignalR hub for real-time notification updates.
///
/// Each connected user is added to a "group" named after their OrgId.
/// When an admin broadcasts a notification, we send it to that group —
/// so only members of the same org receive it, not everyone on the server.
///
/// Hub groups work like chat rooms:
///   Groups.AddToGroupAsync  → user joins the room
///   Clients.Group(orgId)    → send a message to everyone in the room
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly CurrentUserService _currentUser;

    public NotificationHub(CurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Called automatically when a client connects to /hubs/notifications.
    /// We add them to their org group so they only receive their org's events.
    /// Context.ConnectionId is a unique ID for this specific browser connection.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var orgId = _currentUser.OrgId.ToString();

        // Add this connection to the org group.
        // A user opening two browser tabs = two connections, both added to the group.
        await Groups.AddToGroupAsync(Context.ConnectionId, orgId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called automatically when a client disconnects —
    /// browser tab closed, network lost, user logged out.
    /// SignalR cleans up the connection but we remove from group explicitly.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var orgId = _currentUser.OrgId.ToString();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, orgId);
        await base.OnDisconnectedAsync(exception);
    }
}

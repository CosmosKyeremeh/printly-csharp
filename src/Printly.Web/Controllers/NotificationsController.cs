using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Core.DTOs.Requests;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;

namespace Printly.Web.Controllers;

[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;
    private readonly CurrentUserService _currentUser;

    public NotificationsController(
        INotificationService notificationService,
        CurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// GET /api/notifications
    /// Returns all org notifications with IsRead flag per user.
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetNotifications()
        => ExecuteAsync(() => _notificationService.GetOrgNotificationsAsync(
            _currentUser.OrgId, _currentUser.UserId));

    /// <summary>
    /// GET /api/notifications/unread-count
    /// Returns just the integer count for the bell badge.
    /// </summary>
    [HttpGet("unread-count")]
    public Task<IActionResult> GetUnreadCount()
        => ExecuteAsync(() => _notificationService.GetUnreadCountAsync(
            _currentUser.UserId, _currentUser.OrgId));

    /// <summary>
    /// POST /api/notifications
    /// Admin only — broadcast a notification to all org members.
    /// </summary>
    [Authorize(Roles = "Admin,Superadmin,PlatformOwner")]
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateNotificationRequest request)
        => ExecuteAsync(() => _notificationService.CreateNotificationAsync(
            request, _currentUser.UserId, _currentUser.OrgId));

    /// <summary>
    /// POST /api/notifications/read
    /// Body: { notificationIds: [...] }
    /// Marks the given notifications as read for the current user.
    /// </summary>
    [HttpPost("read")]
    public Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
        => ExecuteAsync(() => _notificationService.MarkAsReadAsync(
            request.NotificationIds, _currentUser.UserId));
}

using Microsoft.EntityFrameworkCore;
using Printly.Core.DTOs.Requests;
using Printly.Core.DTOs.Responses;
using Printly.Core.Entities;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Data;

namespace Printly.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly PrintlyDbContext _context;
    private readonly INotificationPusher _pusher;

    public NotificationService(
        PrintlyDbContext context,
        INotificationPusher pusher)
    {
        _context = context;
        _pusher = pusher;
    }

    public async Task<List<NotificationResponse>> GetOrgNotificationsAsync(
        Guid orgId, Guid userId)
    {
        var notifications = await _context.Notifications
            .Include(n => n.CreatedBy)
            .Where(n => n.OrgId == orgId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(n => new NotificationResponse
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type.ToString(),
            IsRead = n.ReadByUserIds.Contains(userId),
            CreatedByName = n.CreatedBy?.FullName ?? "Admin",
            CreatedAt = n.CreatedAt
        }).ToList();
    }

    public async Task<NotificationResponse> CreateNotificationAsync(
        CreateNotificationRequest request,
        Guid adminUserId,
        Guid orgId)
    {
        var notification = new Notification
        {
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            OrgId = orgId,
            CreatedByUserId = adminUserId
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        await _pusher.PushToOrgAsync(orgId.ToString(), "ReceiveNotification", new
        {
            title = notification.Title,
            message = notification.Message,
            type = notification.Type.ToString(),
            createdAt = notification.CreatedAt
        });

        return new NotificationResponse
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type.ToString(),
            IsRead = false,
            CreatedAt = notification.CreatedAt
        };
    }

    public async Task MarkAsReadAsync(List<Guid> notificationIds, Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => notificationIds.Contains(n.Id))
            .ToListAsync();

        foreach (var notification in notifications)
        {
            if (!notification.ReadByUserIds.Contains(userId))
                notification.ReadByUserIds.Add(userId);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, Guid orgId)
    {
        return await _context.Notifications
            .CountAsync(n =>
                n.OrgId == orgId &&
                !n.ReadByUserIds.Contains(userId));
    }
}

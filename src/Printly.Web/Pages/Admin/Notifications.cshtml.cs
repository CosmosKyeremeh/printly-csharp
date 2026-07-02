using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Printly.Core.DTOs.Responses;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;

namespace Printly.Web.Pages.Admin;

[Authorize(Roles = "Admin,Superadmin,PlatformOwner")]
public class AdminNotificationsModel : PageModel
{
    private readonly INotificationService _notificationService;
    private readonly CurrentUserService _currentUser;

    public AdminNotificationsModel(
        INotificationService notificationService,
        CurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    public List<NotificationResponse> Notifications { get; set; } = new();

    public async Task OnGetAsync()
    {
        Notifications = await _notificationService.GetOrgNotificationsAsync(
            _currentUser.OrgId, _currentUser.UserId);
    }
}

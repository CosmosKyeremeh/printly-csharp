using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Printly.Core.DTOs.Responses;
using Printly.Core.Enums;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;

namespace Printly.Web.Pages.Student;

[Authorize(Roles = "Student")]
public class StudentDashboardModel : PageModel
{
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;
    private readonly CurrentUserService _currentUser;

    public StudentDashboardModel(
        IFileService fileService,
        INotificationService notificationService,
        CurrentUserService currentUser)
    {
        _fileService = fileService;
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    public List<FileResponse> RecentFiles { get; set; } = new();
    public List<NotificationResponse> Notifications { get; set; } = new();
    public int TotalFiles { get; set; }
    public int QueuedFiles { get; set; }
    public int PrintingFiles { get; set; }
    public int DoneFiles { get; set; }

    public async Task OnGetAsync()
    {
        RecentFiles = await _fileService.GetUserFilesAsync(
            _currentUser.UserId, _currentUser.OrgId);

        Notifications = await _notificationService.GetOrgNotificationsAsync(
            _currentUser.OrgId, _currentUser.UserId);

        TotalFiles   = RecentFiles.Count;
        QueuedFiles  = RecentFiles.Count(f => f.Status == FileStatus.Queued.ToString());
        PrintingFiles= RecentFiles.Count(f => f.Status == FileStatus.Printing.ToString());
        DoneFiles    = RecentFiles.Count(f => f.Status == FileStatus.Done.ToString());
    }
}

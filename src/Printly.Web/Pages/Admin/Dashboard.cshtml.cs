using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Printly.Core.DTOs.Responses;
using Printly.Core.Enums;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Printly.Infrastructure.Data;

namespace Printly.Web.Pages.Admin;

[Authorize(Roles = "Admin,Superadmin,PlatformOwner")]
public class AdminDashboardModel : PageModel
{
    private readonly IQueueService _queueService;
    private readonly IUserService _userService;
    private readonly CurrentUserService _currentUser;
    private readonly PrintlyDbContext _context;

    public AdminDashboardModel(
        IQueueService queueService,
        IUserService userService,
        CurrentUserService currentUser,
        PrintlyDbContext context)
    {
        _queueService = queueService;
        _userService = userService;
        _currentUser = currentUser;
        _context = context;
    }

    public string OrgName { get; set; } = string.Empty;
    public string JoinCode { get; set; } = string.Empty;
    public int QueuedCount { get; set; }
    public int PrintingCount { get; set; }
    public int DoneCount { get; set; }
    public int TotalUsers { get; set; }
    public List<QueueItemResponse> RecentQueue { get; set; } = new();

    public async Task OnGetAsync()
    {
        var org = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == _currentUser.OrgId);

        OrgName  = org?.Name ?? "Your Class";
        JoinCode = org?.JoinCode ?? "—";

        RecentQueue = await _queueService.GetQueueAsync(_currentUser.OrgId);
        var users   = await _userService.GetOrgUsersAsync(_currentUser.OrgId);

        QueuedCount   = RecentQueue.Count(q => q.Status == FileStatus.Queued.ToString());
        PrintingCount = RecentQueue.Count(q => q.Status == FileStatus.Printing.ToString());
        DoneCount     = RecentQueue.Count(q => q.Status == FileStatus.Done.ToString());
        TotalUsers    = users.Count(u => u.Role == "Student");

        RecentQueue = RecentQueue.Take(10).ToList();
    }
}

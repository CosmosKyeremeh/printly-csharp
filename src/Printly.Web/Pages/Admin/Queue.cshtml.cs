using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Printly.Core.DTOs.Responses;
using Printly.Core.Enums;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;

namespace Printly.Web.Pages.Admin;

[Authorize(Roles = "Admin,Superadmin,PlatformOwner")]
public class AdminQueueModel : PageModel
{
    private readonly IQueueService _queueService;
    private readonly CurrentUserService _currentUser;

    public AdminQueueModel(IQueueService queueService, CurrentUserService currentUser)
    {
        _queueService = queueService;
        _currentUser = currentUser;
    }

    public List<QueueItemResponse> QueueItems { get; set; } = new();

    public async Task OnGetAsync([FromQuery] FileStatus? status)
    {
        QueueItems = await _queueService.GetQueueAsync(_currentUser.OrgId, status);
    }
}

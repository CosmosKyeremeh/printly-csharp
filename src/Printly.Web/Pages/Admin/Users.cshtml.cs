using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Printly.Core.DTOs.Responses;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Data;
using Printly.Infrastructure.Services;

namespace Printly.Web.Pages.Admin;

[Authorize(Roles = "Admin,Superadmin,PlatformOwner")]
public class AdminUsersModel : PageModel
{
    private readonly IUserService _userService;
    private readonly CurrentUserService _currentUser;
    private readonly PrintlyDbContext _context;

    public AdminUsersModel(
        IUserService userService,
        CurrentUserService currentUser,
        PrintlyDbContext context)
    {
        _userService = userService;
        _currentUser = currentUser;
        _context = context;
    }

    public List<UserResponse> Users { get; set; } = new();
    public string JoinCode { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        Users = await _userService.GetOrgUsersAsync(_currentUser.OrgId);

        var org = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == _currentUser.OrgId);
        JoinCode = org?.JoinCode ?? "—";
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Printly.Core.DTOs.Requests;
using Printly.Core.Entities;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Data;

namespace Printly.Web.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly PrintlyDbContext _context;

    public RegisterModel(
        IAuthService authService,
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        PrintlyDbContext context)
    {
        _authService = authService;
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
    }

    public string? ErrorMessage { get; set; }

    public bool IsFirstAccount { get; set; }

    public async Task OnGetAsync()
    {
        IsFirstAccount = !await _context.Organizations.AnyAsync();
    }

    public async Task<IActionResult> OnPostAsync(
        string fullName, string email, string password, string? joinCode, string? orgName)
    {
        IsFirstAccount = !await _context.Organizations.AnyAsync();

        try
        {
            var result = await _authService.RegisterAsync(new RegisterRequest
            {
                FullName = fullName,
                Email = email,
                Password = password,
                JoinCode = joinCode,
                OrgName = orgName
            });

            // Sign in with cookie immediately after registration
            var user = await _userManager.FindByEmailAsync(email);
            await _signInManager.SignInAsync(user!, isPersistent: true);

            Response.Cookies.Append("printly_token", result.Token, new CookieOptions
            {
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = result.ExpiresAt
            });

            return result.User.Role switch
            {
                "Admin" or "Superadmin" or "PlatformOwner"
                    => RedirectToPage("/Admin/Dashboard"),
                _ => RedirectToPage("/Student/Dashboard")
            };
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Printly.Core.DTOs.Requests;
using Printly.Core.Entities;
using Printly.Core.Interfaces;

namespace Printly.Web.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public LoginModel(
        IAuthService authService,
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager)
    {
        _authService = authService;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string email, string password)
    {
        try
        {
            // Validate credentials via our AuthService (generates JWT too)
            var result = await _authService.LoginAsync(new LoginRequest
            {
                Email = email,
                Password = password
            });

            // Also sign in with cookie so Razor Pages work
            // isPersistent = true means the cookie survives browser restart
            var user = await _userManager.FindByEmailAsync(email);
            await _signInManager.SignInAsync(user!, isPersistent: true);

            // Store JWT in cookie for API calls from JS
            Response.Cookies.Append("printly_token", result.Token, new CookieOptions
            {
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = result.ExpiresAt
            });

            // Redirect based on role
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

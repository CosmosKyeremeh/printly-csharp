using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Printly.Core.DTOs.Requests;
using Printly.Core.Entities;
using Printly.Core.Interfaces;

namespace Printly.Web.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public RegisterModel(
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

    public async Task<IActionResult> OnPostAsync(
        string fullName, string email, string password, string? joinCode)
    {
        try
        {
            var result = await _authService.RegisterAsync(new RegisterRequest
            {
                FullName = fullName,
                Email = email,
                Password = password,
                JoinCode = joinCode
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

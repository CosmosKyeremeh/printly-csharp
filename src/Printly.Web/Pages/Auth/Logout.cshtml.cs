using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Printly.Core.Entities;

namespace Printly.Web.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;

    public LogoutModel(SignInManager<AppUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await _signInManager.SignOutAsync();
        Response.Cookies.Delete("printly_token");
        return RedirectToPage("/Auth/Login");
    }
}

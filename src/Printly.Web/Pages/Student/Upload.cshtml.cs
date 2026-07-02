using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Printly.Core.DTOs.Responses;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;

namespace Printly.Web.Pages.Student;

[Authorize(Roles = "Student")]
public class StudentUploadModel : PageModel
{
    private readonly ICategoryService _categoryService;
    private readonly CurrentUserService _currentUser;

    public StudentUploadModel(ICategoryService categoryService, CurrentUserService currentUser)
    {
        _categoryService = categoryService;
        _currentUser = currentUser;
    }

    public List<CategoryResponse> Categories { get; set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await _categoryService.GetCategoriesAsync(_currentUser.OrgId);
    }
}

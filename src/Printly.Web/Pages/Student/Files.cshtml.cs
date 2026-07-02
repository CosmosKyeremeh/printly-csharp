using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Printly.Core.DTOs.Responses;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;

namespace Printly.Web.Pages.Student;

[Authorize(Roles = "Student")]
public class StudentFilesModel : PageModel
{
    private readonly IFileService _fileService;
    private readonly CurrentUserService _currentUser;

    public StudentFilesModel(IFileService fileService, CurrentUserService currentUser)
    {
        _fileService = fileService;
        _currentUser = currentUser;
    }

    public List<FileResponse> Files { get; set; } = new();

    public async Task OnGetAsync()
    {
        Files = await _fileService.GetUserFilesAsync(_currentUser.UserId, _currentUser.OrgId);
    }
}

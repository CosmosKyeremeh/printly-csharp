using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printly.Core.DTOs.Requests;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Services;

namespace Printly.Web.Controllers;

[Authorize]
public class CategoriesController : BaseApiController
{
    private readonly ICategoryService _categoryService;
    private readonly CurrentUserService _currentUser;

    public CategoriesController(
        ICategoryService categoryService,
        CurrentUserService currentUser)
    {
        _categoryService = categoryService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public Task<IActionResult> GetCategories()
        => ExecuteAsync(() => _categoryService.GetCategoriesAsync(_currentUser.OrgId));

    [Authorize(Roles = "Admin,Superadmin,PlatformOwner")]
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        => ExecuteAsync(() => _categoryService.CreateCategoryAsync(
            request, _currentUser.UserId, _currentUser.OrgId));

    [Authorize(Roles = "Admin,Superadmin,PlatformOwner")]
    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] CreateCategoryRequest request)
        => ExecuteAsync(() => _categoryService.UpdateCategoryAsync(
            id, request, _currentUser.OrgId));

    [Authorize(Roles = "Admin,Superadmin,PlatformOwner")]
    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
        => ExecuteAsync(() => _categoryService.DeleteCategoryAsync(id, _currentUser.OrgId));
}

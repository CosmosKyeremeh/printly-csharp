using Printly.Core.DTOs.Requests;
using Printly.Core.DTOs.Responses;

namespace Printly.Core.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryResponse>> GetCategoriesAsync(Guid orgId);
    Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, Guid adminUserId, Guid orgId);
    Task<CategoryResponse> UpdateCategoryAsync(Guid categoryId, CreateCategoryRequest request, Guid orgId);
    Task DeleteCategoryAsync(Guid categoryId, Guid orgId);
}

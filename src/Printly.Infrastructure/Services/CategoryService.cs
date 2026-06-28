using Microsoft.EntityFrameworkCore;
using Printly.Core.DTOs.Requests;
using Printly.Core.DTOs.Responses;
using Printly.Core.Entities;
using Printly.Core.Interfaces;
using Printly.Infrastructure.Data;

namespace Printly.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly PrintlyDbContext _context;

    public CategoryService(PrintlyDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryResponse>> GetCategoriesAsync(Guid orgId)
    {
        var categories = await _context.Categories
            .Include(c => c.Files)
            .Where(c => c.OrgId == orgId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories.Select(c => new CategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Deadline = c.Deadline,
            FileCount = c.Files.Count,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<CategoryResponse> CreateCategoryAsync(
        CreateCategoryRequest request, Guid adminUserId, Guid orgId)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            Deadline = request.Deadline,
            OrgId = orgId,
            CreatedByUserId = adminUserId
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Deadline = category.Deadline,
            FileCount = 0,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(
        Guid categoryId, CreateCategoryRequest request, Guid orgId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.OrgId == orgId)
            ?? throw new InvalidOperationException("Category not found.");

        category.Name = request.Name;
        category.Description = request.Description;
        category.Deadline = request.Deadline;

        await _context.SaveChangesAsync();

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Deadline = category.Deadline,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task DeleteCategoryAsync(Guid categoryId, Guid orgId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.OrgId == orgId)
            ?? throw new InvalidOperationException("Category not found.");

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }
}

using InventoryAPI.Data;
using InventoryAPI.DTOs;
using InventoryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Services;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponseDto>> GetAllAsync();
    Task<CategoryResponseDto?> GetByIdAsync(int id);
    Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto);
    Task<CategoryResponseDto?> UpdateAsync(int id, UpdateCategoryDto dto);
    Task<bool> DeleteAsync(int id);
}

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    public CategoryService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync() =>
        await _db.Categories
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count(p => p.IsActive),
                CreatedAt = c.CreatedAt
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<CategoryResponseDto?> GetByIdAsync(int id)
    {
        var c = await _db.Categories
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return null;
        return new CategoryResponseDto
        {
            Id = c.Id, Name = c.Name, Description = c.Description,
            ImageUrl = c.ImageUrl, IsActive = c.IsActive,
            ProductCount = c.Products.Count(p => p.IsActive),
            CreatedAt = c.CreatedAt
        };
    }

    public async Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto)
    {
        var cat = new Category
        {
            Name = dto.Name,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            CreatedAt = DateTime.UtcNow
        };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return new CategoryResponseDto { Id = cat.Id, Name = cat.Name,
            Description = cat.Description, IsActive = cat.IsActive, CreatedAt = cat.CreatedAt };
    }

    public async Task<CategoryResponseDto?> UpdateAsync(int id, UpdateCategoryDto dto)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat is null) return null;
        if (dto.Name is not null) cat.Name = dto.Name;
        if (dto.Description is not null) cat.Description = dto.Description;
        if (dto.ImageUrl is not null) cat.ImageUrl = dto.ImageUrl;
        if (dto.IsActive.HasValue) cat.IsActive = dto.IsActive.Value;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cat = await _db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (cat is null) return false;
        if (cat.Products.Any(p => p.IsActive))
            throw new InvalidOperationException("Kategoria ka produkte aktive. Hiqini produktet fillimisht.");
        cat.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }
}

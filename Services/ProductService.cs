using System.Text.Json;
using InventoryAPI.Data;
using InventoryAPI.DTOs;
using InventoryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db) => _db = db;

    // ── GET ALL (Paginated + Filtered) ──────────────────────────────────────
    public async Task<PagedResult<ProductResponseDto>> GetProductsAsync(ProductFilterDto f)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .AsQueryable();

        // Filters
        if (!string.IsNullOrWhiteSpace(f.Search))
            query = query.Where(p =>
                p.Name.Contains(f.Search) ||
                p.SKU.Contains(f.Search) ||
                (p.Description != null && p.Description.Contains(f.Search)) ||
                (p.Barcode != null && p.Barcode.Contains(f.Search)));

        if (f.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == f.CategoryId.Value);

        if (f.IsActive.HasValue)
            query = query.Where(p => p.IsActive == f.IsActive.Value);

        if (f.IsLowStock == true)
            query = query.Where(p => p.StockQuantity <= p.LowStockThreshold && p.StockQuantity > 0);

        if (f.IsOutOfStock == true)
            query = query.Where(p => p.StockQuantity == 0);

        if (f.MinPrice.HasValue)
            query = query.Where(p => p.Price >= f.MinPrice.Value);

        if (f.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= f.MaxPrice.Value);

        // Sorting
        query = f.SortBy?.ToLower() switch
        {
            "price"     => f.SortOrder == "desc" ? query.OrderByDescending(p => (double)p.Price) : query.OrderBy(p => (double)p.Price),
            "stock"     => f.SortOrder == "desc" ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity),
            "createdat" => f.SortOrder == "desc" ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _           => f.SortOrder == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((f.Page - 1) * f.PageSize)
            .Take(f.PageSize)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                Description = p.Description,
                Price = p.Price,
                CostPrice = p.CostPrice,
                StockQuantity = p.StockQuantity,
                LowStockThreshold = p.LowStockThreshold,
                ImageUrl = p.ImageUrl,
                Barcode = p.Barcode,
                IsActive = p.IsActive,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return new PagedResult<ProductResponseDto>
        {
            Items = items,
            TotalCount = total,
            Page = f.Page,
            PageSize = f.PageSize
        };
    }

    public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
    {
        var p = await _db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? null : MapToDto(p);
    }

    public async Task<ProductResponseDto?> GetProductBySkuAsync(string sku)
    {
        var p = await _db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.SKU == sku);
        return p is null ? null : MapToDto(p);
    }

    // ── CREATE ──────────────────────────────────────────────────────────────
    public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto, string createdBy)
    {
        var product = new Product
        {
            Name = dto.Name,
            SKU = dto.SKU,
            Description = dto.Description,
            Price = dto.Price,
            CostPrice = dto.CostPrice,
            StockQuantity = dto.StockQuantity,
            LowStockThreshold = dto.LowStockThreshold,
            ImageUrl = dto.ImageUrl,
            Barcode = dto.Barcode,
            CategoryId = dto.CategoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            ProductId = product.Id,
            Action = "CREATE",
            ChangedBy = createdBy,
            NewValues = JsonSerializer.Serialize(dto),
            QuantityAfter = dto.StockQuantity,
            Notes = "Produkt i ri u krijua"
        });
        await _db.SaveChangesAsync();

        await _db.Entry(product).Reference(p => p.Category).LoadAsync();
        return MapToDto(product);
    }

    // ── UPDATE ──────────────────────────────────────────────────────────────
    public async Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto, string updatedBy)
    {
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return null;

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            product.Name, product.Price, product.CostPrice,
            product.LowStockThreshold, product.ImageUrl,
            product.Barcode, product.CategoryId, product.IsActive
        });

        if (dto.Name is not null) product.Name = dto.Name;
        if (dto.Description is not null) product.Description = dto.Description;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.CostPrice.HasValue) product.CostPrice = dto.CostPrice.Value;
        if (dto.LowStockThreshold.HasValue) product.LowStockThreshold = dto.LowStockThreshold.Value;
        if (dto.ImageUrl is not null) product.ImageUrl = dto.ImageUrl;
        if (dto.Barcode is not null) product.Barcode = dto.Barcode;
        if (dto.CategoryId.HasValue) product.CategoryId = dto.CategoryId.Value;
        if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
        product.UpdatedAt = DateTime.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            ProductId = product.Id,
            Action = "UPDATE",
            ChangedBy = updatedBy,
            OldValues = oldSnapshot,
            NewValues = JsonSerializer.Serialize(dto)
        });

        await _db.SaveChangesAsync();
        await _db.Entry(product).Reference(p => p.Category).LoadAsync();
        return MapToDto(product);
    }

    // ── DELETE (soft delete) ─────────────────────────────────────────────────
    public async Task<bool> DeleteProductAsync(int id, string deletedBy)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return false;

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            ProductId = product.Id,
            Action = "DELETE",
            ChangedBy = deletedBy,
            Notes = "Produkt u çaktivizua (soft delete)"
        });

        await _db.SaveChangesAsync();
        return true;
    }

    // ── STOCK ADJUSTMENT ─────────────────────────────────────────────────────
    public async Task<ProductResponseDto?> AdjustStockAsync(int id, StockAdjustmentDto dto, string adjustedBy)
    {
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return null;

        var before = product.StockQuantity;
        var after = before + dto.Quantity;
        if (after < 0) throw new InvalidOperationException($"Sasia e stokut nuk mund të bëhet negative. Aktuale: {before}");

        product.StockQuantity = after;
        product.UpdatedAt = DateTime.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            ProductId = product.Id,
            Action = "STOCK_CHANGE",
            ChangedBy = adjustedBy,
            QuantityBefore = before,
            QuantityAfter = after,
            Notes = dto.Reason
        });

        await _db.SaveChangesAsync();
        return MapToDto(product);
    }

    public async Task<IEnumerable<ProductResponseDto>> GetLowStockProductsAsync() =>
        await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.StockQuantity <= p.LowStockThreshold && p.StockQuantity > 0)
            .OrderBy(p => p.StockQuantity)
            .Select(p => MapToDto(p))
            .ToListAsync();

    public async Task<IEnumerable<ProductResponseDto>> GetOutOfStockProductsAsync() =>
        await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.StockQuantity == 0)
            .OrderBy(p => p.Name)
            .Select(p => MapToDto(p))
            .ToListAsync();

    // ── Helper ───────────────────────────────────────────────────────────────
    private static ProductResponseDto MapToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        SKU = p.SKU,
        Description = p.Description,
        Price = p.Price,
        CostPrice = p.CostPrice,
        StockQuantity = p.StockQuantity,
        LowStockThreshold = p.LowStockThreshold,
        ImageUrl = p.ImageUrl,
        Barcode = p.Barcode,
        IsActive = p.IsActive,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name ?? "",
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}

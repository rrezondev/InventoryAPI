using System.ComponentModel.DataAnnotations;

namespace InventoryAPI.DTOs;

// ── CREATE ──────────────────────────────────────────────────────────────────
public class CreateProductDto
{
    [Required, MinLength(2), MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Price must be positive")]
    public decimal Price { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CostPrice { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int LowStockThreshold { get; set; } = 10;

    public string? ImageUrl { get; set; }
    public string? Barcode { get; set; }

    [Required]
    public int CategoryId { get; set; }
}

// ── UPDATE ──────────────────────────────────────────────────────────────────
public class UpdateProductDto
{
    [MinLength(2), MaxLength(200)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? Price { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CostPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int? LowStockThreshold { get; set; }

    public string? ImageUrl { get; set; }
    public string? Barcode { get; set; }
    public int? CategoryId { get; set; }
    public bool? IsActive { get; set; }
}

// ── STOCK ADJUSTMENT ────────────────────────────────────────────────────────
public class StockAdjustmentDto
{
    [Required]
    public int Quantity { get; set; }   // positive = add, negative = remove

    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

// ── RESPONSE ─────────────────────────────────────────────────────────────────
public class ProductResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsLowStock => StockQuantity <= LowStockThreshold;
    public bool IsOutOfStock => StockQuantity == 0;
    public string? ImageUrl { get; set; }
    public string? Barcode { get; set; }
    public bool IsActive { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ── FILTER / SEARCH ──────────────────────────────────────────────────────────
public class ProductFilterDto
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsLowStock { get; set; }
    public bool? IsOutOfStock { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; } = "name";   // name | price | stock | createdAt
    public string? SortOrder { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ── PAGINATED RESPONSE ────────────────────────────────────────────────────────
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

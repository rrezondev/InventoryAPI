using InventoryAPI.DTOs;

namespace InventoryAPI.Services;

public interface IProductService
{
    Task<PagedResult<ProductResponseDto>> GetProductsAsync(ProductFilterDto filter);
    Task<ProductResponseDto?> GetProductByIdAsync(int id);
    Task<ProductResponseDto?> GetProductBySkuAsync(string sku);
    Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto, string createdBy);
    Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto, string updatedBy);
    Task<bool> DeleteProductAsync(int id, string deletedBy);
    Task<ProductResponseDto?> AdjustStockAsync(int id, StockAdjustmentDto dto, string adjustedBy);
    Task<IEnumerable<ProductResponseDto>> GetLowStockProductsAsync();
    Task<IEnumerable<ProductResponseDto>> GetOutOfStockProductsAsync();
}

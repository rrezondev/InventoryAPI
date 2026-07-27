using System.Security.Claims;
using InventoryAPI.DTOs;
using InventoryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("📦 Produktet & Inventari")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _svc;
    public ProductsController(IProductService svc) => _svc = svc;

    private string CurrentUser => User.FindFirstValue(ClaimTypes.Name) ?? "system";

    // ── GET ALL ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Merr listën e produkteve me paginim, kërkim dhe filtra të avancuara
    /// </summary>
    /// <remarks>
    /// Parametrat e filtrimit:
    /// - **search**: Kërkon në emër, SKU, përshkrim dhe barcode
    /// - **categoryId**: Filtro sipas kategorisë
    /// - **isLowStock**: Vetëm produktet me stok të ulët
    /// - **isOutOfStock**: Vetëm produktet pa stok
    /// - **minPrice / maxPrice**: Filtrimi sipas çmimit
    /// - **sortBy**: name | price | stock | createdAt
    /// - **page / pageSize**: Paginim (default: faqja 1, 20 artikuj)
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ProductResponseDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] ProductFilterDto filter)
        => Ok(await _svc.GetProductsAsync(filter));

    // ── GET BY ID ────────────────────────────────────────────────────────────
    /// <summary>Merr produktin sipas ID</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _svc.GetProductByIdAsync(id);
        return product is null ? NotFound(new { message = $"Produkti me ID {id} nuk u gjet." }) : Ok(product);
    }

    // ── GET BY SKU ────────────────────────────────────────────────────────────
    /// <summary>Merr produktin sipas SKU (kodit unik)</summary>
    [HttpGet("sku/{sku}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBySku(string sku)
    {
        var product = await _svc.GetProductBySkuAsync(sku);
        return product is null ? NotFound(new { message = $"Produkti me SKU '{sku}' nuk u gjet." }) : Ok(product);
    }

    // ── LOW STOCK ALERT ───────────────────────────────────────────────────────
    /// <summary>Merr listën e produkteve me stok të ulët (nën pragun e caktuar)</summary>
    [HttpGet("alerts/low-stock")]
    [ProducesResponseType(typeof(IEnumerable<ProductResponseDto>), 200)]
    public async Task<IActionResult> GetLowStock()
        => Ok(await _svc.GetLowStockProductsAsync());

    // ── OUT OF STOCK ──────────────────────────────────────────────────────────
    /// <summary>Merr listën e produkteve pa stok (0 njësi)</summary>
    [HttpGet("alerts/out-of-stock")]
    [ProducesResponseType(typeof(IEnumerable<ProductResponseDto>), 200)]
    public async Task<IActionResult> GetOutOfStock()
        => Ok(await _svc.GetOutOfStockProductsAsync());

    // ── CREATE ────────────────────────────────────────────────────────────────
    /// <summary>Krijo produkt të ri në inventar</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ProductResponseDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        try
        {
            var product = await _svc.CreateProductAsync(dto, CurrentUser);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            return Conflict(new { message = "SKU ekziston tashmë. Zgjidhni një SKU tjetër." });
        }
    }

    // ── UPDATE ────────────────────────────────────────────────────────────────
    /// <summary>Përditëso të dhënat e produktit (partial update - vetëm fushat e dërguara ndryshojnë)</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ProductResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var product = await _svc.UpdateProductAsync(id, dto, CurrentUser);
        return product is null ? NotFound(new { message = $"Produkti me ID {id} nuk u gjet." }) : Ok(product);
    }

    // ── DELETE ────────────────────────────────────────────────────────────────
    /// <summary>Çaktivizo produktin (soft delete - produkti ruhet në DB me IsActive=false)</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _svc.DeleteProductAsync(id, CurrentUser);
        return deleted ? NoContent() : NotFound(new { message = $"Produkti me ID {id} nuk u gjet." });
    }

    // ── STOCK ADJUSTMENT ──────────────────────────────────────────────────────
    /// <summary>
    /// Rregullon sasinë e stokut (shtim ose ulje)
    /// </summary>
    /// <remarks>
    /// Shembull:
    /// - **quantity: +50** → shto 50 njësi
    /// - **quantity: -10** → hiq 10 njësi (kthim, dëmtim, shitje manuale)
    /// </remarks>
    [HttpPatch("{id:int}/stock")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ProductResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AdjustStock(int id, [FromBody] StockAdjustmentDto dto)
    {
        try
        {
            var product = await _svc.AdjustStockAsync(id, dto, CurrentUser);
            return product is null ? NotFound(new { message = $"Produkti me ID {id} nuk u gjet." }) : Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

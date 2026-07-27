using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("📋 Auditimi (Log i Ndryshimeve)")]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _db;
    public AuditController(AppDbContext db) => _db = db;

    /// <summary>Merr të gjitha loglat e auditimit me filtra</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(IEnumerable<AuditLog>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? productId,
        [FromQuery] string? action,
        [FromQuery] string? changedBy,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.AuditLogs
            .Include(a => a.Product)
            .AsQueryable();

        if (productId.HasValue) query = query.Where(a => a.ProductId == productId);
        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action == action.ToUpper());
        if (!string.IsNullOrEmpty(changedBy)) query = query.Where(a => a.ChangedBy == changedBy);
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);

        var total = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.ProductId,
                ProductName = a.Product.Name,
                a.Action,
                a.ChangedBy,
                a.QuantityBefore,
                a.QuantityAfter,
                a.Notes,
                a.Timestamp
            })
            .ToListAsync();

        return Ok(new
        {
            items = logs,
            totalCount = total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    /// <summary>Merr historikun e ndryshimeve për një produkt specifik</summary>
    [HttpGet("product/{productId:int}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var logs = await _db.AuditLogs
            .Where(a => a.ProductId == productId)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new
            {
                a.Id,
                a.Action,
                a.ChangedBy,
                a.OldValues,
                a.NewValues,
                a.QuantityBefore,
                a.QuantityAfter,
                a.Notes,
                a.Timestamp
            })
            .ToListAsync();

        return Ok(logs);
    }

    /// <summary>Statistikat e aktivitetit (sa ndryshime sipas llojit)</summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStatistics([FromQuery] int days = 30)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var stats = await _db.AuditLogs
            .Where(a => a.Timestamp >= from)
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToListAsync();

        var topChanged = await _db.AuditLogs
            .Where(a => a.Timestamp >= from)
            .GroupBy(a => new { a.ProductId, a.Product.Name })
            .Select(g => new { g.Key.ProductId, g.Key.Name, Changes = g.Count() })
            .OrderByDescending(x => x.Changes)
            .Take(10)
            .ToListAsync();

        return Ok(new { period = $"Últimet {days} ditë", actionStats = stats, topChangedProducts = topChanged });
    }
}

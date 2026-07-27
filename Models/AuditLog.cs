namespace InventoryAPI.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Action { get; set; } = string.Empty;       // CREATE | UPDATE | DELETE | STOCK_CHANGE
    public string? ChangedBy { get; set; }
    public string? OldValues { get; set; }   // JSON snapshot
    public string? NewValues { get; set; }   // JSON snapshot
    public int? QuantityBefore { get; set; }
    public int? QuantityAfter { get; set; }
    public string? Notes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

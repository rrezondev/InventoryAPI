using InventoryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("Manager");
        });

        // ── Category ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Name).IsUnique();
        });

        // ── Product ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(p => p.SKU).IsUnique();
            e.Property(p => p.Price).HasPrecision(18, 2);
            e.Property(p => p.CostPrice).HasPrecision(18, 2);

            e.HasOne(p => p.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(p => p.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AuditLog ─────────────────────────────────────────────────────────
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasOne(a => a.Product)
             .WithMany(p => p.AuditLogs)
             .HasForeignKey(a => a.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seed Data ────────────────────────────────────────────────────────
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Elektronikë", Description = "Pajisje elektronike dhe aksesorë", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "Veshje", Description = "Veshje dhe aksesorë mode", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Name = "Ushqim & Pije", Description = "Produkte ushqimore", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 4, Name = "Libra & Shkollore", Description = "Libra, fletore dhe material shkollor", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
    }
}

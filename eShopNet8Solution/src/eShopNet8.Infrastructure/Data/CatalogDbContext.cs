using eShopNet8.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace eShopNet8.Infrastructure.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<CatalogItem> CatalogItems { get; set; }
    public DbSet<CatalogBrand> CatalogBrands { get; set; }
    public DbSet<CatalogType> CatalogTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CatalogType configuration
        modelBuilder.Entity<CatalogType>(entity =>
        {
            entity.ToTable("CatalogType");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(100);
        });

        // CatalogBrand configuration
        modelBuilder.Entity<CatalogBrand>(entity =>
        {
            entity.ToTable("CatalogBrand");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Brand)
                .IsRequired()
                .HasMaxLength(100);
        });

        // CatalogItem configuration
        modelBuilder.Entity<CatalogItem>(entity =>
        {
            entity.ToTable("Catalog");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Description)
                .HasMaxLength(500);
            
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            entity.Property(e => e.PictureFileName)
                .HasMaxLength(255);
            
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("GETUTCDATE()");

            // Ignore computed properties
            entity.Ignore(e => e.PictureUri);
            entity.Ignore(e => e.TempImageName);

            // Relationships
            entity.HasOne(e => e.CatalogBrand)
                .WithMany(b => b.CatalogItems)
                .HasForeignKey(e => e.CatalogBrandId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CatalogType)
                .WithMany(t => t.CatalogItems)
                .HasForeignKey(e => e.CatalogTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.CatalogBrandId);
            entity.HasIndex(e => e.CatalogTypeId);
            entity.HasIndex(e => e.Name);
        });
    }
}
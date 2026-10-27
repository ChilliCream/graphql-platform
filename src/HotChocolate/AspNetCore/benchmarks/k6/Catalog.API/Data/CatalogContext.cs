using eShop.Catalog.Data.Entities;
using eShop.Catalog.Data.EntityConfigurations;

namespace eShop.Catalog.Data;

public class CatalogContext(DbContextOptions<CatalogContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products { get; set; }

    public DbSet<ProductTypeEntity> ProductTypes { get; set; }

    public DbSet<BrandEntity> Brands { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new BrandEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductTypeEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductEntityTypeConfiguration());
    }
}

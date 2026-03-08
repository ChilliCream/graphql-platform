using eShop.Catalog.Data.Entities;
using eShop.Catalog.Data.EntityConfigurations;

namespace eShop.Catalog.Data;

public class CatalogContext(DbContextOptions<CatalogContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    public DbSet<ProductTypeEntity> ProductTypes => Set<ProductTypeEntity>();

    public DbSet<BrandEntity> Brands => Set<BrandEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new BrandEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductTypeEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductEntityTypeConfiguration());
    }
}

using HotChocolate.Data.Data.EntityConfigurations;
using HotChocolate.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Data;

public class CatalogContext(DbContextOptions<CatalogContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }

    public DbSet<ProductType> ProductTypes { get; set; }

    public DbSet<Brand> Brands { get; set; }

    public DbSet<Supplier> Suppliers { get; set; }

    public DbSet<SingleProperty> SingleProperties { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new BrandEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductTypeEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductEntityTypeConfiguration());
        builder.ApplyConfiguration(new SupplierEntityTypeConfiguration());
    }
}

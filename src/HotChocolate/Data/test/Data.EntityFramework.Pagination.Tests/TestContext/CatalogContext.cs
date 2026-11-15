using HotChocolate.Data.TestContext.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.TestContext;

public class CatalogContext(string connectionString) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(connectionString);
    }

    public DbSet<Product> Products { get; set; }

    public DbSet<ProductType> ProductTypes { get; set; }

    public DbSet<Brand> Brands { get; set; }

    public DbSet<Test> Tests { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new BrandEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductTypeEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductEntityTypeConfiguration());

        // Add the outbox table to this context
        // builder.UseIntegrationEventLogs();
    }
}

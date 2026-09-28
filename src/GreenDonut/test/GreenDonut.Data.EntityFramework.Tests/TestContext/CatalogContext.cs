using GreenDonut.Data.TestContext.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GreenDonut.Data.TestContext;

public class CatalogContext(string connectionString, IEnumerable<IInterceptor>? interceptors = null) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(connectionString);

        if (interceptors is not null)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }
    }

    public DbSet<Product> Products { get; set; } = null!;

    public DbSet<ProductType> ProductTypes { get; set; } = null!;

    public DbSet<Brand> Brands { get; set; } = null!;

    public DbSet<Test> Tests { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new BrandEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductTypeEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductEntityTypeConfiguration());

        // Add the outbox table to this context
        // builder.UseIntegrationEventLogs();
    }
}

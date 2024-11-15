using HotChocolate.Data.TestContext.EntityConfigurations;
using Microsoft.EntityFrameworkCore;


namespace HotChocolate.Data.TestContext;

public class CatalogContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var randomDbName = Guid.NewGuid().ToString("N");
        optionsBuilder.UseSqlite($"Data Source={randomDbName}.db");
    }

    public DbSet<Product> Products { get; set; } = default!;

    public DbSet<ProductType> ProductTypes { get; set; } = default!;

    public DbSet<Brand> Brands { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new BrandEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductTypeEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductEntityTypeConfiguration());

        // Add the outbox table to this context
        // builder.UseIntegrationEventLogs();
    }

    public async Task SeedAsync()
    {
        await Database.EnsureCreatedAsync();

        var type = new ProductType { Name = "T-Shirt", };
        ProductTypes.Add(type);

        for (var i = 0; i < 100; i++)
        {
            var brand = new Brand
            {
                Name = "Brand" + i,
                DisplayName = i % 2 == 0 ? "BrandDisplay" + i : null,
                Details = new() { Country = new() { Name = "Country" + i } }
            };
            Brands.Add(brand);

            for (var j = 0; j < 100; j++)
            {
                var product = new Product
                {
                    Name = $"Product {i}-{j}",
                    Type = type,
                    Brand = brand,
                };
                Products.Add(product);
            }
        }

        await SaveChangesAsync();
    }
}

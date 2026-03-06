using Demo.Catalog.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Mocha.Outbox;
using Mocha.Sagas.EfCore;

namespace Demo.Catalog.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<OrderRecord> Orders => Set<OrderRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddPostgresSagas();
        modelBuilder.AddPostgresOutbox();

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasOne(e => e.Category).WithMany(c => c.Products).HasForeignKey(e => e.CategoryId);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<OrderRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ShippingAddress).HasMaxLength(500).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
        });

        // Seed some sample products
        var electronicsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var booksId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        modelBuilder
            .Entity<Category>()
            .HasData(
                new Category
                {
                    Id = electronicsId,
                    Name = "Electronics",
                    Description = "Electronic devices and accessories"
                },
                new Category
                {
                    Id = booksId,
                    Name = "Books",
                    Description = "Physical and digital books"
                });

        var date = new DateTimeOffset(
            new DateTime(2026, 1, 4, 23, 11, 57, 852, DateTimeKind.Utc),
            new TimeSpan(0, 0, 0, 0, 0));

        modelBuilder
            .Entity<Product>()
            .HasData(
                new Product
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Name = "Wireless Headphones",
                    Description = "Premium noise-cancelling wireless headphones",
                    Price = 299.99m,
                    StockQuantity = 50,
                    CategoryId = electronicsId,
                    CreatedAt = date,
                    UpdatedAt = date
                },
                new Product
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Name = "Mechanical Keyboard",
                    Description = "RGB mechanical gaming keyboard",
                    Price = 149.99m,
                    StockQuantity = 100,
                    CategoryId = electronicsId,
                    CreatedAt = date,
                    UpdatedAt = date
                },
                new Product
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    Name = "Clean Code",
                    Description = "A Handbook of Agile Software Craftsmanship by Robert C. Martin",
                    Price = 39.99m,
                    StockQuantity = 200,
                    CategoryId = booksId,
                    CreatedAt = date,
                    UpdatedAt = date
                });
    }
}

public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=catalog-db;Username=postgres;Password=postgres");
        return new CatalogDbContext(optionsBuilder.Options);
    }
}

using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// The single Postgres-backed fixture shared by every family that proves a native batch
/// middleware against a real database. Cursor and offset paging partitioning is proven with the
/// same plain in-memory shapes the landed hc-0-bpl.4 dependency already validated, so only
/// filtering and sorting (whose predicates genuinely need to prove SQL translation) seed data
/// here. Each family owns its own brand/product entity pair so that its GraphQL type names never
/// collide with another family's, but both are seeded and queried through this one context.
/// </summary>
public sealed class BatchDbContext(DbContextOptions<BatchDbContext> options) : DbContext(options)
{
    public DbSet<FilteringBrand> FilteringBrands => Set<FilteringBrand>();

    public DbSet<FilteringProduct> FilteringProducts => Set<FilteringProduct>();

    public DbSet<SortingBrand> SortingBrands => Set<SortingBrand>();

    public DbSet<SortingProduct> SortingProducts => Set<SortingProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FilteringBrand>()
            .HasMany(b => b.Products)
            .WithOne(p => p.Brand)
            .HasForeignKey(p => p.BrandId);

        modelBuilder.Entity<SortingBrand>()
            .HasMany(b => b.Products)
            .WithOne(p => p.Brand)
            .HasForeignKey(p => p.BrandId);
    }
}

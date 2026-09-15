using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// The single Postgres-backed fixture shared by every family that proves a native batch
/// middleware against a real database. Filtering, sorting, cursor paging, offset paging, and the
/// <c>PageConnection&lt;T&gt;</c> family all seed real brand/product rows here and load them
/// through this context; their nested batch resolvers then slice, filter, sort, or page the
/// already-loaded <c>Products</c> navigation in memory, the same shape a real per-parent resolver
/// would use once its parent has eagerly included its children. The projection family seeds only
/// its brand row here: its nested batch children stay a shared plain in-memory
/// <c>IQueryable&lt;ProjectionProduct&gt;</c> unrelated to this context, so that projecting only
/// the root query (<c>[UseProjection] IQueryable&lt;ProjectionBrand&gt;</c>) is the thing under
/// test rather than an EF <c>Include</c>/projection interaction. Each family owns its own
/// brand/product entity pair so that its GraphQL type names never collide with another family's.
/// </summary>
public sealed class BatchDbContext(DbContextOptions<BatchDbContext> options) : DbContext(options)
{
    public DbSet<FilteringBrand> FilteringBrands => Set<FilteringBrand>();

    public DbSet<FilteringProduct> FilteringProducts => Set<FilteringProduct>();

    public DbSet<SortingBrand> SortingBrands => Set<SortingBrand>();

    public DbSet<SortingProduct> SortingProducts => Set<SortingProduct>();

    public DbSet<CursorBrand> CursorBrands => Set<CursorBrand>();

    public DbSet<CursorProduct> CursorProducts => Set<CursorProduct>();

    public DbSet<OffsetBrand> OffsetBrands => Set<OffsetBrand>();

    public DbSet<OffsetProduct> OffsetProducts => Set<OffsetProduct>();

    public DbSet<PageConnectionBrand> PageConnectionBrands => Set<PageConnectionBrand>();

    public DbSet<PageConnectionProduct> PageConnectionProducts => Set<PageConnectionProduct>();

    public DbSet<ProjectionBrand> ProjectionBrands => Set<ProjectionBrand>();

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

        modelBuilder.Entity<CursorBrand>()
            .HasMany(b => b.Products)
            .WithOne(p => p.Brand)
            .HasForeignKey(p => p.BrandId);

        modelBuilder.Entity<OffsetBrand>()
            .HasMany(b => b.Products)
            .WithOne(p => p.Brand)
            .HasForeignKey(p => p.BrandId);

        modelBuilder.Entity<PageConnectionBrand>()
            .HasMany(b => b.Products)
            .WithOne(p => p.Brand)
            .HasForeignKey(p => p.BrandId);
    }
}

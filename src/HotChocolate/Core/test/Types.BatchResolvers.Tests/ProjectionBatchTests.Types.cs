using System.ComponentModel.DataAnnotations;
using HotChocolate.Data;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class ProjectionBatchTests
{
    private static void Common(IRequestExecutorBuilder builder)
        => builder.AddFiltering().AddSorting().AddProjections();

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType<ProjectionAttributeQuery>()
            .AddTypeExtension<ProjectionBrandAttributeExtension>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType(ProjectionRootQuery.Initialize)
            .AddObjectType<ProjectionBrand>(ProjectionBrandNode.Initialize);
    }

    private void ConfigureFluent(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("brands")
                    .UseProjection()
                    .Resolve(ctx => ctx.Service<BatchDbContext>().ProjectionBrands);
            })
            .AddType(new ObjectType<ProjectionBrand>(d =>
            {
                d.Field("filteredProducts")
                    .Type<ListType<ObjectType<ProjectionProduct>>>()
                    .UseFiltering<ProjectionProduct>()
                    .ResolveBatch(contexts =>
                    {
                        Probe.Record("GetFilteredProducts", contexts.Select(c => c.Parent<ProjectionBrand>().Id));
                        var results = contexts
                            .Select(c => ResolverResult.Ok(
                                ProjectionQuery.ProductsFor(c.Parent<ProjectionBrand>(), Probe)))
                            .ToArray();
                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });
                d.Field("sortedProducts")
                    .Type<ListType<ObjectType<ProjectionProduct>>>()
                    .UseSorting<ProjectionProduct>()
                    .ResolveBatch(contexts =>
                    {
                        Probe.Record("GetSortedProducts", contexts.Select(c => c.Parent<ProjectionBrand>().Id));
                        var results = contexts
                            .Select(c => ResolverResult.Ok(
                                ProjectionQuery.ProductsFor(c.Parent<ProjectionBrand>(), Probe)))
                            .ToArray();
                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });
                d.Field("projectedProducts")
                    .Type<ListType<ObjectType<ProjectionProduct>>>()
                    .UseProjection<ProjectionProduct>()
                    .ResolveBatch(contexts =>
                    {
                        Probe.Record("GetProjectedProducts", contexts.Select(c => c.Parent<ProjectionBrand>().Id));
                        var results = contexts
                            .Select(c => ResolverResult.Ok(
                                ProjectionQuery.ProductsFor(c.Parent<ProjectionBrand>(), Probe)))
                            .ToArray();
                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });
            }));
    }

    private async Task<string> SeedAsync(CancellationToken cancellationToken)
        => _connectionString = await _resource.CreateSeededDatabaseAsync(
            static async (context, _) =>
            {
                context.ProjectionBrands.AddRange(
                    new ProjectionBrand { Id = 1, Name = "Brand 1" },
                    new ProjectionBrand { Id = 2, Name = "Brand 2" });
                await Task.CompletedTask;
            },
            cancellationToken);
}

/// <summary>
/// The batch children under a projected parent are not themselves EF-backed: only the root
/// brands query is the subject of the projection assertion here (a real <c>IQueryable</c> from
/// Postgres, captured as SQL), so the children stay the simplest possible plain in-memory
/// <c>IQueryable</c> in every declaration style, unrelated to the <see cref="BatchDbContext"/>.
/// </summary>
public static class ProjectionQuery
{
    public static IQueryable<ProjectionProduct> ProductsFor(ProjectionBrand brand, BatchProbe probe)
        => new RecordingQueryable<ProjectionProduct>(
            [
                new ProjectionProduct { Name = $"{brand.Name} P1", Unselected = "secret" },
                new ProjectionProduct { Name = $"{brand.Name} P2", Unselected = "secret" }
            ],
            probe);
}

/// <summary>
/// A Postgres-backed brand whose <c>[UseProjection]</c> root query proves the native projection
/// batch middleware (hc-0-bpl.3) alongside a real EF <c>IQueryable</c> pipeline.
/// </summary>
public sealed class ProjectionBrand
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;
}

public sealed class ProjectionProduct
{
    public string Name { get; set; } = null!;

    public string? Unselected { get; set; }
}

public sealed class ProjectionAttributeQuery
{
    [UseProjection]
    public IQueryable<ProjectionBrand> GetBrands([Service] BatchDbContext db) => db.ProjectionBrands;
}

[ExtendObjectType<ProjectionBrand>]
public sealed class ProjectionBrandAttributeExtension
{
    [UseFiltering]
    [BatchResolver]
    public List<IQueryable<ProjectionProduct>> GetFilteredProducts(
        [Parent] List<ProjectionBrand> brands,
        BatchProbe probe)
    {
        probe.Record("GetFilteredProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => ProjectionQuery.ProductsFor(b, probe));
    }

    [UseSorting]
    [BatchResolver]
    public List<IQueryable<ProjectionProduct>> GetSortedProducts(
        [Parent] List<ProjectionBrand> brands,
        BatchProbe probe)
    {
        probe.Record("GetSortedProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => ProjectionQuery.ProductsFor(b, probe));
    }

    [UseProjection]
    [BatchResolver]
    public List<IQueryable<ProjectionProduct>> GetProjectedProducts(
        [Parent] List<ProjectionBrand> brands,
        BatchProbe probe)
    {
        probe.Record("GetProjectedProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => ProjectionQuery.ProductsFor(b, probe));
    }
}

[QueryType]
public static partial class ProjectionRootQuery
{
    [UseProjection]
    public static IQueryable<ProjectionBrand> GetBrands([Service] BatchDbContext db) => db.ProjectionBrands;
}

[ObjectType<ProjectionBrand>]
public static partial class ProjectionBrandNode
{
    [UseFiltering]
    [BatchResolver]
    public static List<IQueryable<ProjectionProduct>> GetFilteredProducts(
        [Parent] List<ProjectionBrand> brands,
        BatchProbe probe)
    {
        probe.Record("GetFilteredProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => ProjectionQuery.ProductsFor(b, probe));
    }

    [UseSorting]
    [BatchResolver]
    public static List<IQueryable<ProjectionProduct>> GetSortedProducts(
        [Parent] List<ProjectionBrand> brands,
        BatchProbe probe)
    {
        probe.Record("GetSortedProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => ProjectionQuery.ProductsFor(b, probe));
    }

    [UseProjection]
    [BatchResolver]
    public static List<IQueryable<ProjectionProduct>> GetProjectedProducts(
        [Parent] List<ProjectionBrand> brands,
        BatchProbe probe)
    {
        probe.Record("GetProjectedProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => ProjectionQuery.ProductsFor(b, probe));
    }
}

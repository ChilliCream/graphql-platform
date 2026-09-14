using HotChocolate.Data;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class ProjectionBatchTests
{
    private static void Common(IRequestExecutorBuilder builder)
        => builder.AddFiltering().AddSorting().AddProjections();

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder.AddQueryType<ProjectionAttributeQuery>().AddTypeExtension<ProjectionBrandAttributeExtension>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddQueryType(ProjectionRootQuery.Initialize)
            .AddObjectType<ProjectionBrand>(ProjectionBrandNode.Initialize);
    }

    private void ConfigureFluent(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("brands").UseProjection().Resolve(_ => ProjectionQuery.Brands);
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
                            .Select(c => ResolverResult.Ok(ProjectionQuery.ProductsFor(c.Parent<ProjectionBrand>())))
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
                            .Select(c => ResolverResult.Ok(ProjectionQuery.ProductsFor(c.Parent<ProjectionBrand>())))
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
                            .Select(c => ResolverResult.Ok(ProjectionQuery.ProductsFor(c.Parent<ProjectionBrand>())))
                            .ToArray();
                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });
            }));
    }
}

/// <summary>
/// A shared plain (non-EF) IQueryable data source: only the batch children under it are the
/// subject of these scenarios, so the parent stays the simplest possible projectable source in
/// every declaration style.
/// </summary>
public static class ProjectionQuery
{
    public static IQueryable<ProjectionBrand> Brands
        => new[] { new ProjectionBrand { Id = 1, Name = "Brand 1" }, new ProjectionBrand { Id = 2, Name = "Brand 2" } }
            .AsQueryable();

    public static IQueryable<ProjectionProduct> ProductsFor(ProjectionBrand brand)
        => new[]
        {
            new ProjectionProduct { Name = $"{brand.Name} P1", Unselected = "secret" },
            new ProjectionProduct { Name = $"{brand.Name} P2", Unselected = "secret" }
        }.AsQueryable();
}

public sealed class ProjectionBrand
{
    public int Id { get; set; }

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
    public IQueryable<ProjectionBrand> GetBrands() => ProjectionQuery.Brands;
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
        return brands.ConvertAll(ProjectionQuery.ProductsFor);
    }

    [UseSorting]
    [BatchResolver]
    public List<IQueryable<ProjectionProduct>> GetSortedProducts(
        [Parent] List<ProjectionBrand> brands,
        BatchProbe probe)
    {
        probe.Record("GetSortedProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(ProjectionQuery.ProductsFor);
    }

    [UseProjection]
    [BatchResolver]
    public List<IQueryable<ProjectionProduct>> GetProjectedProducts(
        [Parent] List<ProjectionBrand> brands,
        BatchProbe probe)
    {
        probe.Record("GetProjectedProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(ProjectionQuery.ProductsFor);
    }
}

[QueryType]
public static partial class ProjectionRootQuery
{
    [UseProjection]
    public static IQueryable<ProjectionBrand> GetBrands() => ProjectionQuery.Brands;
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
        return brands.ConvertAll(ProjectionQuery.ProductsFor);
    }

    [UseSorting]
    [BatchResolver]
    public static List<IQueryable<ProjectionProduct>> GetSortedProducts(
        [Parent] List<ProjectionBrand> brands,
        BatchProbe probe)
    {
        probe.Record("GetSortedProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(ProjectionQuery.ProductsFor);
    }

    [UseProjection]
    [BatchResolver]
    public static List<IQueryable<ProjectionProduct>> GetProjectedProducts(
        [Parent] List<ProjectionBrand> brands,
        BatchProbe probe)
    {
        probe.Record("GetProjectedProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(ProjectionQuery.ProductsFor);
    }
}

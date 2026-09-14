using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class FilteringBatchTests
{
    private static void Common(IRequestExecutorBuilder builder) => builder.AddFiltering();

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType<FilteringAttributeQuery>()
            .AddTypeExtension<FilteringBrandAttributeExtension>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType(FilteringQuery.Initialize)
            .AddObjectType<FilteringBrand>(FilteringBrandNode.Initialize);
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
                    .Type<ListType<ObjectType<FilteringBrand>>>()
                    .Resolve(ctx => ctx.Service<BatchDbContext>().FilteringBrands
                        .Include(b => b.Products)
                        .OrderBy(b => b.Id)
                        .ToListAsync(ctx.RequestAborted));
            })
            .AddType(new ObjectType<FilteringBrand>(d =>
            {
                d.Field("products")
                    .Type<NonNullType<ListType<NonNullType<ObjectType<FilteringProduct>>>>>()
                    .UseFiltering<FilteringProduct>()
                    .ResolveBatch(contexts =>
                    {
                        Probe.Record("GetProducts", contexts.Select(c => c.Parent<FilteringBrand>().Id));
                        var results = new ResolverResult[contexts.Count];

                        for (var i = 0; i < contexts.Count; i++)
                        {
                            results[i] = ResolverResult.Ok(contexts[i].Parent<FilteringBrand>().Products.ToArray());
                        }

                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });
                d.Field("predicateProducts")
                    .Type<ListType<ObjectType<FilteringProduct>>>()
                    .UseFiltering<FilteringProduct>()
                    .ResolveBatch(contexts =>
                    {
                        Probe.Record("GetPredicateProducts", contexts.Select(c => c.Parent<FilteringBrand>().Id));
                        // Every context in a partition shares the same predicate (same argument
                        // value for the whole occurrence), so binding it once from the first
                        // context is enough, mirroring the attribute/source-gen shape.
                        var predicate = contexts.Count > 0
                            ? contexts[0].GetFilterContext()?.AsPredicate<FilteringProduct>()
                            : null;
                        Probe.Record("Predicate", [predicate?.ToString()]);
                        var results = new ResolverResult[contexts.Count];

                        for (var i = 0; i < contexts.Count; i++)
                        {
                            var products = contexts[i].Parent<FilteringBrand>().Products;

                            results[i] = ResolverResult.Ok(predicate is null
                                ? products.ToArray()
                                : products.Where(predicate.Compile()).ToArray());
                        }

                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });
            }));
    }

    private async Task<string> SeedAsync(CancellationToken cancellationToken)
        => _connectionString = await _resource.CreateSeededDatabaseAsync(
            static async (context, ct) =>
            {
                context.FilteringBrands.AddRange(
                    new FilteringBrand
                    {
                        Id = 1,
                        Name = "Brand 1",
                        Products =
                        [
                            new FilteringProduct { Name = "P1" },
                            new FilteringProduct { Name = "P2" }
                        ]
                    },
                    new FilteringBrand
                    {
                        Id = 2,
                        Name = "Brand 2",
                        Products =
                        [
                            new FilteringProduct { Name = "P1" },
                            new FilteringProduct { Name = "P2" }
                        ]
                    });
                await Task.CompletedTask;
            },
            cancellationToken);
}

/// <summary>
/// A Postgres-backed brand whose products are resolved through a native filtering batch
/// middleware. Kept distinct from every other family's brand type so their GraphQL schemas never
/// collide even though each is built independently.
/// </summary>
public sealed class FilteringBrand
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public List<FilteringProduct> Products { get; set; } = [];
}

public sealed class FilteringProduct
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public int BrandId { get; set; }

    [ForeignKey(nameof(BrandId))]
    public FilteringBrand? Brand { get; set; }
}

/// <summary>
/// Attribute-style root query resolving brands from Postgres, eagerly including their products so
/// the nested batch resolver never issues a second query.
/// </summary>
public sealed class FilteringAttributeQuery
{
    public Task<List<FilteringBrand>> GetBrands([Service] BatchDbContext db, CancellationToken cancellationToken)
        => db.FilteringBrands.Include(b => b.Products).OrderBy(b => b.Id).ToListAsync(cancellationToken);
}

/// <summary>
/// Attribute-style batch resolver: filters the already-loaded per-brand products in memory, the
/// same shape as every other declaration style, so <c>UseFiltering</c> pushes its predicate onto
/// each entry's result.
/// </summary>
[ExtendObjectType<FilteringBrand>]
public sealed class FilteringBrandAttributeExtension
{
    [UseFiltering]
    [BatchResolver]
    public List<FilteringProduct[]> GetProducts([Parent] List<FilteringBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => b.Products.ToArray());
    }

    /// <summary>
    /// Same shape as <see cref="GetProducts"/>, but reads the filter predicate itself through
    /// <see cref="IFilterContext"/> instead of letting <c>UseFiltering</c> apply it after the
    /// resolver returns: every entry in a partition shares the same predicate, so binding it once
    /// from the first context (the batch resolver compiler's default for a bare custom parameter)
    /// still produces the correct per-entry result.
    /// </summary>
    [UseFiltering]
    [BatchResolver]
    public List<FilteringProduct[]> GetPredicateProducts(
        [Parent] List<FilteringBrand> brands,
        IFilterContext filterContext,
        BatchProbe probe)
    {
        probe.Record("GetPredicateProducts", brands.Select(b => b.Id));
        var predicate = filterContext.AsPredicate<FilteringProduct>();
        probe.Record("Predicate", [predicate?.ToString()]);
        return brands.ConvertAll(b => predicate is null
            ? b.Products.ToArray()
            : b.Products.Where(predicate.Compile()).ToArray());
    }
}

[QueryType]
public static partial class FilteringQuery
{
    public static Task<List<FilteringBrand>> GetBrands([Service] BatchDbContext db, CancellationToken cancellationToken)
        => db.FilteringBrands.Include(b => b.Products).OrderBy(b => b.Id).ToListAsync(cancellationToken);
}

[ObjectType<FilteringBrand>]
public static partial class FilteringBrandNode
{
    [UseFiltering]
    [BatchResolver]
    public static List<FilteringProduct[]> GetProducts([Parent] List<FilteringBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => b.Products.ToArray());
    }

    [UseFiltering]
    [BatchResolver]
    public static List<FilteringProduct[]> GetPredicateProducts(
        [Parent] List<FilteringBrand> brands,
        IFilterContext filterContext,
        BatchProbe probe)
    {
        probe.Record("GetPredicateProducts", brands.Select(b => b.Id));
        var predicate = filterContext.AsPredicate<FilteringProduct>();
        probe.Record("Predicate", [predicate?.ToString()]);
        return brands.ConvertAll(b => predicate is null
            ? b.Products.ToArray()
            : b.Products.Where(predicate.Compile()).ToArray());
    }
}

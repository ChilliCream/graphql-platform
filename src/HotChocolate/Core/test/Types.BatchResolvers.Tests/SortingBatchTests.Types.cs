using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotChocolate.Data;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class SortingBatchTests
{
    private static void Common(IRequestExecutorBuilder builder) => builder.AddSorting();

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType<SortingAttributeQuery>()
            .AddTypeExtension<SortingBrandAttributeExtension>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType(SortingQuery.Initialize)
            .AddObjectType<SortingBrand>(SortingBrandNode.Initialize);
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
                    .Type<ListType<ObjectType<SortingBrand>>>()
                    .Resolve(ctx => ctx.Service<BatchDbContext>().SortingBrands
                        .Include(b => b.Products)
                        .OrderBy(b => b.Id)
                        .ToListAsync(ctx.RequestAborted));
            })
            .AddType(new ObjectType<SortingBrand>(d => d.Field("products")
                .Type<NonNullType<ListType<NonNullType<ObjectType<SortingProduct>>>>>()
                .UseSorting<SortingProduct>()
                .ResolveBatch(contexts =>
                {
                    Probe.Record("GetProducts", contexts.Select(c => c.Parent<SortingBrand>().Id));
                    var results = new ResolverResult[contexts.Count];

                    for (var i = 0; i < contexts.Count; i++)
                    {
                        results[i] = ResolverResult.Ok(contexts[i].Parent<SortingBrand>().Products.ToArray());
                    }

                    return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                })));
    }

    private async Task<string> SeedAsync(CancellationToken cancellationToken)
        => _connectionString = await _resource.CreateSeededDatabaseAsync(
            static async (context, _) =>
            {
                context.SortingBrands.AddRange(
                    new SortingBrand
                    {
                        Id = 1,
                        Name = "Brand 1",
                        Products = [new SortingProduct { Name = "P1" }, new SortingProduct { Name = "P2" }]
                    },
                    new SortingBrand
                    {
                        Id = 2,
                        Name = "Brand 2",
                        Products = [new SortingProduct { Name = "P1" }, new SortingProduct { Name = "P2" }]
                    });
                await Task.CompletedTask;
            },
            cancellationToken);
}

/// <summary>
/// A Postgres-backed brand whose products are resolved through a native sorting batch middleware.
/// </summary>
public sealed class SortingBrand
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public List<SortingProduct> Products { get; set; } = [];
}

public sealed class SortingProduct
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public int BrandId { get; set; }

    [ForeignKey(nameof(BrandId))]
    public SortingBrand? Brand { get; set; }
}

public sealed class SortingAttributeQuery
{
    public Task<List<SortingBrand>> GetBrands([Service] BatchDbContext db, CancellationToken cancellationToken)
        => db.SortingBrands.Include(b => b.Products).OrderBy(b => b.Id).ToListAsync(cancellationToken);
}

[ExtendObjectType<SortingBrand>]
public sealed class SortingBrandAttributeExtension
{
    [UseSorting]
    [BatchResolver]
    public List<SortingProduct[]> GetProducts([Parent] List<SortingBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => b.Products.ToArray());
    }
}

[QueryType]
public static partial class SortingQuery
{
    public static Task<List<SortingBrand>> GetBrands([Service] BatchDbContext db, CancellationToken cancellationToken)
        => db.SortingBrands.Include(b => b.Products).OrderBy(b => b.Id).ToListAsync(cancellationToken);
}

[ObjectType<SortingBrand>]
public static partial class SortingBrandNode
{
    [UseSorting]
    [BatchResolver]
    public static List<SortingProduct[]> GetProducts([Parent] List<SortingBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => b.Products.ToArray());
    }
}

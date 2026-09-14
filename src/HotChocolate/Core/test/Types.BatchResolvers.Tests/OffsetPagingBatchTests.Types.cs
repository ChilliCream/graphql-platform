using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class OffsetPagingBatchTests
{
    private void ConfigureAttribute(IRequestExecutorBuilder builder)
        => builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType<OffsetAttributeQuery>()
            .AddTypeExtension<OffsetBrandAttributeExtension>();

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
        => builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType(OffsetQuery.Initialize)
            .AddObjectType<OffsetBrand>(OffsetBrandNode.Initialize);

    private void ConfigureFluent(IRequestExecutorBuilder builder)
        => builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("brands")
                    .Type<ListType<ObjectType<OffsetBrand>>>()
                    .Resolve(ctx => ctx.Service<BatchDbContext>().OffsetBrands
                        .Include(b => b.Products)
                        .OrderBy(b => b.Id)
                        .ToListAsync(ctx.RequestAborted));
            })
            .AddObjectType<OffsetBrand>(d => d.Field("products")
                .ResolveBatchWith<FluentOffsetResolvers>(t => t.GetProducts(default!, default!))
                .UseOffsetPaging<ObjectType<OffsetProduct>>());

    private async Task<string> SeedAsync(CancellationToken cancellationToken)
        => _connectionString = await _resource.CreateSeededDatabaseAsync(
            static async (context, _) =>
            {
                context.OffsetBrands.AddRange(
                    new OffsetBrand
                    {
                        Id = 1,
                        Name = "Brand 1",
                        Products =
                        [
                            new OffsetProduct { Name = "Brand 1 P1" },
                            new OffsetProduct { Name = "Brand 1 P2" },
                            new OffsetProduct { Name = "Brand 1 P3" }
                        ]
                    },
                    new OffsetBrand
                    {
                        Id = 2,
                        Name = "Brand 2",
                        Products =
                        [
                            new OffsetProduct { Name = "Brand 2 P1" },
                            new OffsetProduct { Name = "Brand 2 P2" },
                            new OffsetProduct { Name = "Brand 2 P3" }
                        ]
                    });
                await Task.CompletedTask;
            },
            cancellationToken);
}

/// <summary>
/// A Postgres-backed brand whose products are resolved through a native offset paging batch
/// middleware.
/// </summary>
public sealed class OffsetBrand
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public List<OffsetProduct> Products { get; set; } = [];
}

public sealed class OffsetProduct
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public int BrandId { get; set; }

    [ForeignKey(nameof(BrandId))]
    public OffsetBrand? Brand { get; set; }
}

// -- Fluent -----------------------------------------------------------------------------------

public sealed class FluentOffsetResolvers
{
    public List<List<OffsetProduct>> GetProducts([Parent] List<OffsetBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => b.Products);
    }
}

// -- Attribute ----------------------------------------------------------------------------------

public sealed class OffsetAttributeQuery
{
    public Task<List<OffsetBrand>> GetBrands([Service] BatchDbContext db, CancellationToken cancellationToken)
        => db.OffsetBrands.Include(b => b.Products).OrderBy(b => b.Id).ToListAsync(cancellationToken);
}

[ExtendObjectType<OffsetBrand>]
public sealed class OffsetBrandAttributeExtension
{
    [UseOffsetPaging]
    [BatchResolver]
    public List<List<OffsetProduct>> GetProducts([Parent] List<OffsetBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => b.Products);
    }
}

// -- Source generated -----------------------------------------------------------------------

[QueryType]
public static partial class OffsetQuery
{
    public static Task<List<OffsetBrand>> GetBrands([Service] BatchDbContext db, CancellationToken cancellationToken)
        => db.OffsetBrands.Include(b => b.Products).OrderBy(b => b.Id).ToListAsync(cancellationToken);
}

[ObjectType<OffsetBrand>]
public static partial class OffsetBrandNode
{
    [UseOffsetPaging]
    [BatchResolver]
    public static List<List<OffsetProduct>> GetProducts([Parent] List<OffsetBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => b.Products);
    }
}

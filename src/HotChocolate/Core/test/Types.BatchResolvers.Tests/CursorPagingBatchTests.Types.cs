using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GreenDonut.Data;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class CursorPagingBatchTests
{
    private static void Common(IRequestExecutorBuilder builder)
        => builder.AddPagingArguments().ModifyPagingOptions(o =>
        {
            o.DefaultPageSize = 2;
            o.MaxPageSize = 10;
            o.IncludeTotalCount = true;
        });

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType<CursorAttributeQuery>()
            .AddTypeExtension<CursorBrandAttributeExtension>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType(CursorQuery.Initialize)
            .AddObjectType<CursorBrand>(CursorBrandNode.Initialize);
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
                    .Type<ListType<ObjectType<CursorBrand>>>()
                    .Resolve(ctx => ctx.Service<BatchDbContext>().CursorBrands
                        .Include(b => b.Products)
                        .OrderBy(b => b.Id)
                        .ToListAsync(ctx.RequestAborted));
            })
            .AddObjectType<CursorBrand>(d =>
            {
                d.Field("products")
                    .ResolveBatchWith<FluentCursorResolvers>(t => t.GetProducts(default!, default!))
                    .UsePaging<ObjectType<CursorProduct>>();
                d.Field("pagedProducts")
                    .ResolveBatchWith<FluentCursorResolvers>(t => t.GetPagedProducts(default!, default!, default!))
                    .UsePaging<ObjectType<CursorProduct>>();
            });
    }

    private async Task<string> SeedAsync(CancellationToken cancellationToken)
        => _connectionString = await _resource.CreateSeededDatabaseAsync(
            static async (context, _) =>
            {
                context.CursorBrands.AddRange(
                    new CursorBrand
                    {
                        Id = 1,
                        Name = "Brand 1",
                        Products =
                        [
                            new CursorProduct { Name = "Brand 1 P1" },
                            new CursorProduct { Name = "Brand 1 P2" },
                            new CursorProduct { Name = "Brand 1 P3" }
                        ]
                    },
                    new CursorBrand
                    {
                        Id = 2,
                        Name = "Brand 2",
                        Products =
                        [
                            new CursorProduct { Name = "Brand 2 P1" },
                            new CursorProduct { Name = "Brand 2 P2" },
                            new CursorProduct { Name = "Brand 2 P3" }
                        ]
                    });
                await Task.CompletedTask;
            },
            cancellationToken);
}

/// <summary>
/// A Postgres-backed brand whose products are resolved through a native cursor paging batch
/// middleware.
/// </summary>
public sealed class CursorBrand
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public List<CursorProduct> Products { get; set; } = [];
}

public sealed class CursorProduct
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public int BrandId { get; set; }

    [ForeignKey(nameof(BrandId))]
    public CursorBrand? Brand { get; set; }
}

// -- Fluent -----------------------------------------------------------------------------------

public sealed class FluentCursorResolvers
{
    public List<List<CursorProduct>> GetProducts([Parent] List<CursorBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => b.Products);
    }

    public List<Page<CursorProduct>> GetPagedProducts(
        [Parent] List<CursorBrand> brands,
        PagingArguments pagingArguments,
        [Service] BatchProbe probe)
    {
        probe.Record("GetPagedProducts", brands.Select(b => b.Id));
        probe.Record<object?>(
            "PagingArguments",
            [pagingArguments.First, pagingArguments.After, pagingArguments.Last, pagingArguments.Before]);
        return brands.ConvertAll(b => CursorProductFactory.PagedProductsFor(b, pagingArguments));
    }
}

internal static class CursorProductFactory
{
    // Contract: classic [UsePaging] re-pages the returned Page<T> as a plain list and recomputes
    // totalCount/pageInfo from the enumerated items, so the outer result reflects the page size,
    // not brand.Products.Count.
    public static Page<CursorProduct> PagedProductsFor(CursorBrand brand, PagingArguments pagingArguments)
    {
        var count = pagingArguments.First ?? throw new InvalidOperationException("first was not bound");
        var products = brand.Products.Take(count).ToImmutableArray();

        return Page<CursorProduct>.Create(
            products,
            hasNextPage: count < brand.Products.Count,
            hasPreviousPage: false,
            createCursor: product => product.Id.ToString(),
            totalCount: brand.Products.Count);
    }
}

// -- Attribute ----------------------------------------------------------------------------------

public sealed class CursorAttributeQuery
{
    public Task<List<CursorBrand>> GetBrands([Service] BatchDbContext db, CancellationToken cancellationToken)
        => db.CursorBrands.Include(b => b.Products).OrderBy(b => b.Id).ToListAsync(cancellationToken);
}

[ExtendObjectType<CursorBrand>]
public sealed class CursorBrandAttributeExtension
{
    [UsePaging]
    [BatchResolver]
    public List<List<CursorProduct>> GetProducts([Parent] List<CursorBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => b.Products);
    }

    [UsePaging]
    [BatchResolver]
    public List<Page<CursorProduct>> GetPagedProducts(
        [Parent] List<CursorBrand> brands,
        PagingArguments pagingArguments,
        [Service] BatchProbe probe)
    {
        probe.Record("GetPagedProducts", brands.Select(b => b.Id));
        probe.Record<object?>(
            "PagingArguments",
            [pagingArguments.First, pagingArguments.After, pagingArguments.Last, pagingArguments.Before]);
        return brands.ConvertAll(b => CursorProductFactory.PagedProductsFor(b, pagingArguments));
    }
}

// -- Source generated -----------------------------------------------------------------------

[QueryType]
public static partial class CursorQuery
{
    public static Task<List<CursorBrand>> GetBrands([Service] BatchDbContext db, CancellationToken cancellationToken)
        => db.CursorBrands.Include(b => b.Products).OrderBy(b => b.Id).ToListAsync(cancellationToken);
}

[ObjectType<CursorBrand>]
public static partial class CursorBrandNode
{
    [UsePaging]
    [BatchResolver]
    public static List<List<CursorProduct>> GetProducts([Parent] List<CursorBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => b.Products);
    }

    [UsePaging]
    [BatchResolver]
    public static List<Page<CursorProduct>> GetPagedProducts(
        [Parent] List<CursorBrand> brands,
        PagingArguments pagingArguments,
        [Service] BatchProbe probe)
    {
        probe.Record("GetPagedProducts", brands.Select(b => b.Id));
        probe.Record<object?>(
            "PagingArguments",
            [pagingArguments.First, pagingArguments.After, pagingArguments.Last, pagingArguments.Before]);
        return brands.ConvertAll(b => CursorProductFactory.PagedProductsFor(b, pagingArguments));
    }
}

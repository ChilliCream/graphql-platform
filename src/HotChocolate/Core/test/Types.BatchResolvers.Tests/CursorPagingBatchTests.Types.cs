using System.Collections.Immutable;
using GreenDonut.Data;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

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
        builder.AddQueryType<CursorAttributeQuery>().AddTypeExtension<CursorBrandAttributeExtension>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder.AddQueryType(CursorQuery.Initialize).AddObjectType<CursorBrand>(CursorBrandNode.Initialize);
    }

    private void ConfigureFluent(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("brands").Resolve(CursorFixture.Brands);
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
}

public static class CursorFixture
{
    public static List<CursorBrand> Brands =>
    [
        new CursorBrand(1, "Brand 1"),
        new CursorBrand(2, "Brand 2")
    ];
}

public sealed record CursorBrand(int Id, string Name);

public sealed record CursorProduct(int Id, string Name);

// -- Fluent -----------------------------------------------------------------------------------

public sealed class FluentCursorResolvers
{
    public List<List<CursorProduct>> GetProducts([Parent] List<CursorBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(CursorProductFactory.PlainProductsFor);
    }

    public List<Page<CursorProduct>> GetPagedProducts(
        [Parent] List<CursorBrand> brands,
        PagingArguments pagingArguments,
        [Service] BatchProbe probe)
    {
        probe.Record("GetPagedProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => CursorProductFactory.PagedProductsFor(b, pagingArguments));
    }
}

internal static class CursorProductFactory
{
    public static List<CursorProduct> PlainProductsFor(CursorBrand brand)
        =>
        [
            new CursorProduct(1, $"{brand.Name} P1"),
            new CursorProduct(2, $"{brand.Name} P2"),
            new CursorProduct(3, $"{brand.Name} P3")
        ];

    public static Page<CursorProduct> PagedProductsFor(CursorBrand brand, PagingArguments pagingArguments)
    {
        var count = pagingArguments.First ?? 2;
        var products = Enumerable
            .Range(1, count)
            .Select(i => new CursorProduct(i, $"{brand.Name} Product {i}"))
            .ToImmutableArray();

        return Page<CursorProduct>.Create(
            products,
            hasNextPage: false,
            hasPreviousPage: false,
            createCursor: product => product.Id.ToString(),
            totalCount: products.Length);
    }
}

// -- Attribute ----------------------------------------------------------------------------------

public sealed class CursorAttributeQuery
{
    public List<CursorBrand> GetBrands() => CursorFixture.Brands;
}

[ExtendObjectType<CursorBrand>]
public sealed class CursorBrandAttributeExtension
{
    [UsePaging]
    [BatchResolver]
    public List<List<CursorProduct>> GetProducts([Parent] List<CursorBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(CursorProductFactory.PlainProductsFor);
    }

    [UsePaging]
    [BatchResolver]
    public List<Page<CursorProduct>> GetPagedProducts(
        [Parent] List<CursorBrand> brands,
        PagingArguments pagingArguments,
        [Service] BatchProbe probe)
    {
        probe.Record("GetPagedProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => CursorProductFactory.PagedProductsFor(b, pagingArguments));
    }
}

// -- Source generated -----------------------------------------------------------------------

[QueryType]
public static partial class CursorQuery
{
    public static List<CursorBrand> GetBrands() => CursorFixture.Brands;
}

[ObjectType<CursorBrand>]
public static partial class CursorBrandNode
{
    [UsePaging]
    [BatchResolver]
    public static List<List<CursorProduct>> GetProducts([Parent] List<CursorBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(CursorProductFactory.PlainProductsFor);
    }

    [UsePaging]
    [BatchResolver]
    public static List<Page<CursorProduct>> GetPagedProducts(
        [Parent] List<CursorBrand> brands,
        PagingArguments pagingArguments,
        [Service] BatchProbe probe)
    {
        probe.Record("GetPagedProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => CursorProductFactory.PagedProductsFor(b, pagingArguments));
    }
}

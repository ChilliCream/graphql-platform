using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class OffsetPagingBatchTests
{
    private void ConfigureAttribute(IRequestExecutorBuilder builder)
        => builder.AddQueryType<OffsetAttributeQuery>().AddTypeExtension<OffsetBrandAttributeExtension>();

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
        => builder.AddQueryType(OffsetQuery.Initialize).AddObjectType<OffsetBrand>(OffsetBrandNode.Initialize);

    private void ConfigureFluent(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("brands").Resolve(OffsetFixture.Brands);
            })
            .AddObjectType<OffsetBrand>(d => d.Field("products")
                .ResolveBatchWith<FluentOffsetResolvers>(t => t.GetProducts(default!, default!))
                .UseOffsetPaging<ObjectType<OffsetProduct>>());
}

public static class OffsetFixture
{
    public static List<OffsetBrand> Brands =>
    [
        new OffsetBrand(1, "Brand 1"),
        new OffsetBrand(2, "Brand 2")
    ];

    public static List<OffsetProduct> ProductsFor(OffsetBrand brand)
        =>
        [
            new OffsetProduct($"{brand.Name} P1"),
            new OffsetProduct($"{brand.Name} P2"),
            new OffsetProduct($"{brand.Name} P3")
        ];
}

public sealed record OffsetBrand(int Id, string Name);

public sealed record OffsetProduct(string Name);

// -- Fluent -----------------------------------------------------------------------------------

public sealed class FluentOffsetResolvers
{
    public List<List<OffsetProduct>> GetProducts([Parent] List<OffsetBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(OffsetFixture.ProductsFor);
    }
}

// -- Attribute ----------------------------------------------------------------------------------

public sealed class OffsetAttributeQuery
{
    public List<OffsetBrand> GetBrands() => OffsetFixture.Brands;
}

[ExtendObjectType<OffsetBrand>]
public sealed class OffsetBrandAttributeExtension
{
    [UseOffsetPaging]
    [BatchResolver]
    public List<List<OffsetProduct>> GetProducts([Parent] List<OffsetBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(OffsetFixture.ProductsFor);
    }
}

// -- Source generated -----------------------------------------------------------------------

[QueryType]
public static partial class OffsetQuery
{
    public static List<OffsetBrand> GetBrands() => OffsetFixture.Brands;
}

[ObjectType<OffsetBrand>]
public static partial class OffsetBrandNode
{
    [UseOffsetPaging]
    [BatchResolver]
    public static List<List<OffsetProduct>> GetProducts([Parent] List<OffsetBrand> brands, BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(OffsetFixture.ProductsFor);
    }
}

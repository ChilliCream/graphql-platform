using System.Collections.Immutable;
using System.Reflection;
using GreenDonut.Data;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class PageConnectionBatchTests
{
    private static void Common(IRequestExecutorBuilder builder) => builder.AddPagingArguments();

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddQueryType<PageConnectionAttributeQuery>()
            .AddTypeExtension<PageConnectionBrandAttributeExtension>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddQueryType(PageConnectionQuery.Initialize)
            .AddObjectType<PageConnectionBrand>(PageConnectionBrandNode.Initialize);
    }

    private void ConfigureFluent(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("brands").Resolve(PageConnectionFixture.Brands);
            })
            .AddType<PageConnectionProductConnectionType>()
            .AddObjectType<PageConnectionBrand>(d =>
            {
                var field = d.Field("products")
                    .Type<NonNullType<PageConnectionProductConnectionType>>()
                    .Argument("first", a => a.Type<IntType>())
                    .Argument("after", a => a.Type<StringType>())
                    .Argument("last", a => a.Type<IntType>())
                    .Argument("before", a => a.Type<StringType>())
                    .ResolveBatchWith<FluentPageConnectionResolvers>(t => t.GetProducts(default!, default!, default!));
                PageConnectionFixture.ApplyUseConnectionValidation(field);
            });
    }
}

public static class PageConnectionFixture
{
    /// <summary>
    /// Applies the same paging-validation middleware, batch middleware, and partition key that
    /// <see cref="UseConnectionAttribute"/> wires up for a reflection or source-generated
    /// declaration, so the fluent declaration publishes the same
    /// <see cref="GreenDonut.Data.PagingArguments"/> local state; there is no fluent-descriptor
    /// equivalent of the attribute, and it is internal to Types.CursorPagination, so this reaches
    /// it the same way the landed hc-0-bpl.4 dependency test reaches a batch-only internal member.
    /// </summary>
    public static void ApplyUseConnectionValidation(IObjectFieldDescriptor descriptor)
    {
        var attribute = new UseConnectionAttribute();
        var method = typeof(UseConnectionAttribute).GetMethod(
            "TryConfigure",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        method.Invoke(attribute, [descriptor.Extend().Context, descriptor, typeof(PageConnectionBrand)]);
    }

    public static List<PageConnectionBrand> Brands =>
    [
        new PageConnectionBrand(1, "Brand 1"),
        new PageConnectionBrand(2, "Brand 2")
    ];

    public static PageConnection<PageConnectionProduct> ProductsFor(PageConnectionBrand brand, PagingArguments args)
    {
        var count = args.First ?? 2;
        var products = Enumerable
            .Range(1, count)
            .Select(i => new PageConnectionProduct(i, $"{brand.Name} Product {i}"))
            .ToImmutableArray();
        var page = Page<PageConnectionProduct>.Create(
            products,
            hasNextPage: false,
            hasPreviousPage: false,
            createCursor: product => product.Id.ToString(),
            totalCount: products.Length);

        return new PageConnection<PageConnectionProduct>(page);
    }
}

public sealed record PageConnectionBrand(int Id, string Name);

public sealed record PageConnectionProduct(int Id, string Name);

public sealed class PageConnectionProductConnectionType : ObjectType<PageConnection<PageConnectionProduct>>
{
    protected override void Configure(IObjectTypeDescriptor<PageConnection<PageConnectionProduct>> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Name("PageConnectionProductConnection");
        descriptor.Field(t => t.Nodes);
    }
}

// -- Fluent -----------------------------------------------------------------------------------

public sealed class FluentPageConnectionResolvers
{
    public List<PageConnection<PageConnectionProduct>> GetProducts(
        [Parent] List<PageConnectionBrand> brands,
        PagingArguments pagingArguments,
        [Service] BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => PageConnectionFixture.ProductsFor(b, pagingArguments));
    }
}

// -- Attribute ----------------------------------------------------------------------------------

/// <summary>
/// The reflection descriptor for a batch-resolved <c>PageConnection&lt;T&gt;</c> field cannot
/// infer its type and cursor arguments from the return type the way the source generator can
/// (ObjectTypeInspector.cs:353-364 is generator-only), so this attribute wires them explicitly,
/// mirroring the same hand-authored shape the landed hc-0-jyk.2 dependency test already uses.
/// </summary>
public sealed class UsePageConnectionAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
    {
        descriptor.Type<NonNullType<PageConnectionProductConnectionType>>();
        descriptor.Argument("first", a => a.Type<IntType>());
        descriptor.Argument("after", a => a.Type<StringType>());
        descriptor.Argument("last", a => a.Type<IntType>());
        descriptor.Argument("before", a => a.Type<StringType>());
    }
}

public sealed class PageConnectionAttributeQuery
{
    public List<PageConnectionBrand> GetBrands() => PageConnectionFixture.Brands;
}

[ExtendObjectType<PageConnectionBrand>]
public sealed class PageConnectionBrandAttributeExtension
{
    [UseConnection]
    [UsePageConnection]
    [BatchResolver]
    public List<PageConnection<PageConnectionProduct>> GetProducts(
        [Parent] List<PageConnectionBrand> brands,
        PagingArguments pagingArguments,
        [Service] BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => PageConnectionFixture.ProductsFor(b, pagingArguments));
    }
}

// -- Source generated -----------------------------------------------------------------------

[QueryType]
public static partial class PageConnectionQuery
{
    public static List<PageConnectionBrand> GetBrands() => PageConnectionFixture.Brands;
}

/// <summary>
/// Exercises the generator's automatic <c>UseConnection</c> detection for a batch resolver
/// (ObjectTypeInspector.IsBatchConnectionResolver): a <c>List&lt;PageConnection&lt;T&gt;&gt;</c>
/// return type is recognized as a connection resolver at compile time, so no manual type or
/// argument wiring is needed here the way the reflection declaration above needs it.
/// </summary>
[ObjectType<PageConnectionBrand>]
public static partial class PageConnectionBrandNode
{
    [UseConnection]
    [BatchResolver]
    public static List<PageConnection<PageConnectionProduct>> GetProducts(
        [Parent] List<PageConnectionBrand> brands,
        PagingArguments pagingArguments,
        BatchProbe probe)
    {
        probe.Record("GetProducts", brands.Select(b => b.Id));
        return brands.ConvertAll(b => PageConnectionFixture.ProductsFor(b, pagingArguments));
    }
}

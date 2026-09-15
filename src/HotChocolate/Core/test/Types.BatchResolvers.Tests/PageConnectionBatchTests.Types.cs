using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using GreenDonut.Data;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class PageConnectionBatchTests
{
    private static void Common(IRequestExecutorBuilder builder) => builder.AddPagingArguments();

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType<PageConnectionAttributeQuery>()
            .AddTypeExtension<PageConnectionBrandAttributeExtension>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        Common(builder);
        builder
            .AddBatchDbContext(_connectionString, _capturedSql)
            .AddQueryType(PageConnectionQuery.Initialize)
            .AddObjectType<PageConnectionBrand>(PageConnectionBrandNode.Initialize);
    }

    private async Task<string> SeedAsync(CancellationToken cancellationToken)
        => _connectionString = await _resource.CreateSeededDatabaseAsync(
            static async (context, _) =>
            {
                context.PageConnectionBrands.AddRange(
                    new PageConnectionBrand
                    {
                        Id = 1,
                        Name = "Brand 1",
                        Products =
                        [
                            new PageConnectionProduct { Name = "Brand 1 Product 1" },
                            new PageConnectionProduct { Name = "Brand 1 Product 2" },
                            new PageConnectionProduct { Name = "Brand 1 Product 3" }
                        ]
                    },
                    new PageConnectionBrand
                    {
                        Id = 2,
                        Name = "Brand 2",
                        Products =
                        [
                            new PageConnectionProduct { Name = "Brand 2 Product 1" },
                            new PageConnectionProduct { Name = "Brand 2 Product 2" },
                            new PageConnectionProduct { Name = "Brand 2 Product 3" }
                        ]
                    });
                await Task.CompletedTask;
            },
            cancellationToken);
}

public static class PageConnectionFixture
{
    public static PageConnection<PageConnectionProduct> ProductsFor(PageConnectionBrand brand, PagingArguments args)
    {
        var count = args.First ?? 2;
        var products = brand.Products.Take(count).ToImmutableArray();
        var page = Page<PageConnectionProduct>.Create(
            products,
            hasNextPage: count < brand.Products.Count,
            hasPreviousPage: false,
            createCursor: product => product.Id.ToString(),
            totalCount: brand.Products.Count);

        return new PageConnection<PageConnectionProduct>(page);
    }
}

/// <summary>
/// A Postgres-backed brand whose products are resolved through a batch-resolved
/// <c>PageConnection&lt;T&gt;</c> field.
/// </summary>
public sealed class PageConnectionBrand
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public List<PageConnectionProduct> Products { get; set; } = [];
}

public sealed class PageConnectionProduct
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public int BrandId { get; set; }

    [ForeignKey(nameof(BrandId))]
    public PageConnectionBrand? Brand { get; set; }
}

public sealed class PageConnectionProductConnectionType : ObjectType<PageConnection<PageConnectionProduct>>
{
    protected override void Configure(IObjectTypeDescriptor<PageConnection<PageConnectionProduct>> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Name("PageConnectionProductConnection");
        descriptor.Field(t => t.Nodes);
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
    public Task<List<PageConnectionBrand>> GetBrands([Service] BatchDbContext db, CancellationToken cancellationToken)
        => db.PageConnectionBrands.Include(b => b.Products).OrderBy(b => b.Id).ToListAsync(cancellationToken);
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
    public static Task<List<PageConnectionBrand>> GetBrands(
        [Service] BatchDbContext db,
        CancellationToken cancellationToken)
        => db.PageConnectionBrands.Include(b => b.Products).OrderBy(b => b.Id).ToListAsync(cancellationToken);
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

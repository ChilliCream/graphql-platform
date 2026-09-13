using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class DeferBatchTests
{
    /// <summary>
    /// The fixed set of products every declaration style exposes and resolves against.
    /// </summary>
    public static readonly IReadOnlyList<DeferProduct> Products =
    [
        new DeferProduct(1, "Product 1"),
        new DeferProduct(2, "Product 2")
    ];

    private static void Common(IRequestExecutorBuilder builder)
        => builder.ModifyOptions(o => o.EnableDefer = true);

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
    {
        Common(builder);

        builder.AddQueryType<DeferAttributeQuery>().AddTypeExtension<DeferWrapperAttributeExtension>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        Common(builder);

        builder
            .AddQueryType(DeferQuery.Initialize)
            .AddObjectType<DeferWrapper>(DeferWrapperNode.Initialize);
    }

    private void ConfigureFluent(IRequestExecutorBuilder builder)
    {
        Common(builder);

        builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("ready").Type<StringType>().Resolve("ready");
                d.Field("wrapper").Type<ObjectType<DeferWrapper>>().Resolve(new DeferWrapper());
                d.Field("productById")
                    .Argument("id", a => a.Type<IntType>())
                    .ResolveBatchWith<FluentDeferResolvers>(t => t.GetProductById(null!, null!));
            })
            .AddObjectType<DeferWrapper>(d =>
                d.Field("productById")
                    .Argument("id", a => a.Type<IntType>())
                    .ResolveBatchWith<FluentDeferResolvers>(t => t.GetProductById(null!, null!)));
    }
}

/// <summary>
/// A product resolved through a batch resolver, either directly on the root query or nested
/// under <see cref="DeferWrapper"/>, and delivered from inside a deferred fragment.
/// </summary>
public sealed record DeferProduct(int Id, string Name);

/// <summary>
/// A plain, non-batch wrapper type used to host a batch-resolved field one level below the
/// query root so a deferred fragment can be nested rather than sitting at the root selection.
/// </summary>
public sealed record DeferWrapper;

/// <summary>
/// Fluent-style batch resolver bound with <c>ResolveBatchWith</c> for both the root and the
/// <see cref="DeferWrapper"/>-nested <c>productById</c> field.
/// </summary>
public sealed class FluentDeferResolvers
{
    public List<DeferProduct?> GetProductById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => DeferBatchTests.Products.FirstOrDefault(p => p.Id == i));
    }
}

/// <summary>
/// Attribute-style root query exposing the batch resolver at the query root and the plain
/// fields used to place a deferred fragment at, and below, the root selection.
/// </summary>
public sealed class DeferAttributeQuery
{
    public string GetReady() => "ready";

    public DeferWrapper GetWrapper() => new();

    [BatchResolver]
    public List<DeferProduct?> GetProductById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => DeferBatchTests.Products.FirstOrDefault(p => p.Id == i));
    }
}

/// <summary>
/// Attribute-style batch resolver for <see cref="DeferWrapper"/>, nesting the deferred
/// fragment one level below the query root.
/// </summary>
[ExtendObjectType<DeferWrapper>]
public sealed class DeferWrapperAttributeExtension
{
    [BatchResolver]
    public List<DeferProduct?> GetProductById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => DeferBatchTests.Products.FirstOrDefault(p => p.Id == i));
    }
}

/// <summary>
/// Source-generated root query exposing the batch resolver at the query root and the plain
/// fields used to place a deferred fragment at, and below, the root selection.
/// </summary>
[QueryType]
public static partial class DeferQuery
{
    public static string GetReady() => "ready";

    public static DeferWrapper GetWrapper() => new();

    [BatchResolver]
    public static List<DeferProduct?> GetProductById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => DeferBatchTests.Products.FirstOrDefault(p => p.Id == i));
    }
}

/// <summary>
/// Source-generated batch resolver for <see cref="DeferWrapper"/>, nesting the deferred
/// fragment one level below the query root.
/// </summary>
[ObjectType<DeferWrapper>]
public static partial class DeferWrapperNode
{
    [BatchResolver]
    public static List<DeferProduct?> GetProductById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => DeferBatchTests.Products.FirstOrDefault(p => p.Id == i));
    }
}

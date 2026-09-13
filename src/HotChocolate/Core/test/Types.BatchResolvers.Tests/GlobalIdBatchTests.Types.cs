using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class GlobalIdBatchTests
{
    /// <summary>
    /// The fixed set of products every declaration style exposes and resolves against.
    /// </summary>
    public static readonly IReadOnlyList<IdProduct> Products =
    [
        new IdProduct(1, "Product 1"),
        new IdProduct(2, "Product 2")
    ];

    private static void Common(IRequestExecutorBuilder builder)
        => builder.AddGlobalObjectIdentification(false);

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
    {
        Common(builder);

        builder.AddQueryType<IdAttributeQuery>().AddTypeExtension<IdProductAttributeExtension>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        Common(builder);

        builder
            .AddQueryType(IdQuery.Initialize)
            .AddObjectType<IdProduct>(IdProductNode.Initialize);
    }

    private void ConfigureFluent(IRequestExecutorBuilder builder)
    {
        Common(builder);

        builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("products").Type<ListType<ObjectType<IdProduct>>>().Resolve(Products);
                d.Field("productById")
                    .ResolveBatchWith<FluentIdResolvers>(t => t.GetProductById(null!, null!))
                    .Argument("id", a => a.Type<IntType>().ID("IdProduct"));
            })
            .AddObjectType<IdProduct>(d =>
                d.Field("externalId")
                    .ResolveBatchWith<FluentIdResolvers>(t => t.GetExternalId(null!, null!))
                    .ID("IdProduct"));
    }
}

/// <summary>
/// A product whose identifier is exposed as a Relay global object identifier.
/// </summary>
public sealed record IdProduct([property: ID("IdProduct")] int Id, string Name);

/// <summary>
/// Fluent-style batch resolvers bound with <c>ResolveBatchWith</c>, marked as global ids purely
/// through fluent <c>.ID()</c> and <c>.Argument(...)</c> calls rather than attributes.
/// </summary>
public sealed class FluentIdResolvers
{
    public List<int> GetExternalId([Parent] List<IdProduct> parents, BatchProbe probe)
    {
        var keys = parents.ConvertAll(p => p.Id);
        probe.Record(nameof(GetExternalId), keys);
        return keys;
    }

    public List<IdProduct?> GetProductById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => GlobalIdBatchTests.Products.FirstOrDefault(p => p.Id == i));
    }
}

/// <summary>
/// Attribute-style batch resolver for <see cref="IdProduct.Id"/> as an encoded global id.
/// </summary>
[ExtendObjectType<IdProduct>]
public sealed class IdProductAttributeExtension
{
    [BatchResolver]
    [ID("IdProduct")]
    public List<int> GetExternalId([Parent] List<IdProduct> parents, BatchProbe probe)
    {
        var keys = parents.ConvertAll(p => p.Id);
        probe.Record(nameof(GetExternalId), keys);
        return keys;
    }
}

/// <summary>
/// Attribute-style root query, including a batch resolver that decodes a global id.
/// </summary>
public sealed class IdAttributeQuery
{
    public IReadOnlyList<IdProduct> GetProducts() => GlobalIdBatchTests.Products;

    [BatchResolver]
    public List<IdProduct?> GetProductById([ID] List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => GlobalIdBatchTests.Products.FirstOrDefault(p => p.Id == i));
    }
}

/// <summary>
/// Source-generated batch resolver for <see cref="IdProduct.Id"/> as an encoded global id.
/// </summary>
[ObjectType<IdProduct>]
public static partial class IdProductNode
{
    [BatchResolver]
    [ID("IdProduct")]
    public static List<int> GetExternalId([Parent] List<IdProduct> parents, BatchProbe probe)
    {
        var keys = parents.ConvertAll(p => p.Id);
        probe.Record(nameof(GetExternalId), keys);
        return keys;
    }
}

/// <summary>
/// Source-generated root query, including a batch resolver that decodes a global id.
/// </summary>
[QueryType]
public static partial class IdQuery
{
    public static IReadOnlyList<IdProduct> GetProducts() => GlobalIdBatchTests.Products;

    [BatchResolver]
    public static List<IdProduct?> GetProductById([ID] List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => GlobalIdBatchTests.Products.FirstOrDefault(p => p.Id == i));
    }
}

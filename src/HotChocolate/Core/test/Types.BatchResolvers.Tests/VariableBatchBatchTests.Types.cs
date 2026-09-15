using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class VariableBatchBatchTests
{
    /// <summary>
    /// The fixed set of products every declaration style resolves against. Id 99 is
    /// deliberately absent so a request for it exercises the non-null violation path.
    /// </summary>
    public static readonly IReadOnlyList<VariableBatchProduct> Products =
    [
        new VariableBatchProduct(1, "Product 1"),
        new VariableBatchProduct(2, "Product 2")
    ];

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
        => builder.AddQueryType<VariableBatchAttributeQuery>();

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
        => builder.AddQueryType(VariableBatchQuery.Initialize);

    private void ConfigureFluent(IRequestExecutorBuilder builder)
        => builder.AddQueryType(d =>
        {
            d.Name("Query");
            d.Field("productById")
                .Argument("id", a => a.Type<IntType>())
                .ResolveBatchWith<FluentVariableBatchResolvers>(t => t.GetProductById(null!, null!))
                .Type<NonNullType<ObjectType<VariableBatchProduct>>>();
        });
}

/// <summary>
/// A product resolved through a batch resolver keyed by a per-variable-set argument.
/// </summary>
public sealed record VariableBatchProduct(int Id, string Name);

/// <summary>
/// Fluent-style batch resolver bound with <c>ResolveBatchWith</c>. A missing id resolves to
/// <c>null!</c>, which the inferred non-null field turns into a violation scoped to that
/// entry's own variable set.
/// </summary>
public sealed class FluentVariableBatchResolvers
{
    public List<VariableBatchProduct> GetProductById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => VariableBatchBatchTests.Products.FirstOrDefault(p => p.Id == i) ?? null!);
    }
}

/// <summary>
/// Attribute-style root query whose non-nullable element type alone makes the field non-null.
/// A missing id resolves to <c>null!</c>, which the inferred non-null field turns into a
/// violation scoped to that entry's own variable set.
/// </summary>
public sealed class VariableBatchAttributeQuery
{
    [BatchResolver]
    public List<VariableBatchProduct> GetProductById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => VariableBatchBatchTests.Products.FirstOrDefault(p => p.Id == i) ?? null!);
    }
}

/// <summary>
/// Source-generated root query with a non-null batch resolver. A missing id resolves to
/// <c>null!</c>, which the non-null field turns into a violation scoped to that entry's own
/// variable set.
/// </summary>
[QueryType]
public static partial class VariableBatchQuery
{
    [BatchResolver]
    [GraphQLNonNullType]
    public static List<VariableBatchProduct> GetProductById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetProductById), id);
        return id.ConvertAll(i => VariableBatchBatchTests.Products.FirstOrDefault(p => p.Id == i) ?? null!);
    }
}

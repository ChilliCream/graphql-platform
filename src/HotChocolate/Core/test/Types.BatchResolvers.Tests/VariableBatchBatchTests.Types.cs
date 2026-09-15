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
            d.Field("unionProduct")
                .Argument("id", a => a.Type<IntType>())
                .ResolveBatchWith<FluentVariableBatchResolvers>(
                    t => t.GetUnionProduct(null!, false, false, null!));
        });
}

/// <summary>
/// A product resolved through a batch resolver keyed by a per-variable-set argument.
/// </summary>
public sealed record VariableBatchProduct(int Id, string Name);

/// <summary>
/// A product whose <c>left</c>/<c>right</c> fields report the <c>[IsSelected]</c> flags a
/// single batch dispatch bound for the whole variable batch.
/// </summary>
public sealed record UnionSelectionProduct(int Id, string Left, string Right);

/// <summary>
/// Records the <c>[IsSelected("left")]</c>/<c>[IsSelected("right")]</c> values a single
/// <c>unionProduct</c> batch dispatch was bound with.
/// </summary>
public sealed class IsSelectedUnionLog
{
    public List<IsSelectedUnionObservation> Invocations { get; } = [];
}

public sealed record IsSelectedUnionObservation(IReadOnlyList<int> Ids, bool Left, bool Right);

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

    public List<UnionSelectionProduct> GetUnionProduct(
        List<int> id,
        [IsSelected("left")] bool left,
        [IsSelected("right")] bool right,
        [Service] IsSelectedUnionLog log)
    {
        log.Invocations.Add(new IsSelectedUnionObservation(id, left, right));
        return id.Select(i => new UnionSelectionProduct(
            i,
            left ? "left" : "missing",
            right ? "right" : "missing")).ToList();
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

    [BatchResolver]
    public List<UnionSelectionProduct> GetUnionProduct(
        List<int> id,
        [IsSelected("left")] bool left,
        [IsSelected("right")] bool right,
        [Service] IsSelectedUnionLog log)
    {
        log.Invocations.Add(new IsSelectedUnionObservation(id, left, right));
        return id.Select(i => new UnionSelectionProduct(
            i,
            left ? "left" : "missing",
            right ? "right" : "missing")).ToList();
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

    [BatchResolver]
    public static List<UnionSelectionProduct> GetUnionProduct(
        List<int> id,
        [IsSelected("left")] bool left,
        [IsSelected("right")] bool right,
        [Service] IsSelectedUnionLog log)
    {
        log.Invocations.Add(new IsSelectedUnionObservation(id, left, right));
        return id.Select(i => new UnionSelectionProduct(
            i,
            left ? "left" : "missing",
            right ? "right" : "missing")).ToList();
    }
}

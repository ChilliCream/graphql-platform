namespace HotChocolate.Fusion.Suites.ComplexEntityCall;

/// <summary>
/// Shared seed data for the <c>complex-entity-call</c> audit suite, transcribed
/// from <c>graphql-hive/federation-gateway-audit/src/test-suites/complex-entity-call/data.ts</c>.
/// The same products and categories are referenced from every subgraph; nothing
/// is partitioned by subgraph.
/// </summary>
internal static class ComplexEntityCallData
{
    public sealed record ProductRecord(string Id, string Pid, string CategoryId, double Price);

    public sealed record CategoryRecord(string Id, string Tag, string MainProduct);

    /// <summary>
    /// The seeded products, in source order.
    /// </summary>
    public static readonly IReadOnlyList<ProductRecord> Products =
    [
        new ProductRecord("1", "p1", "c1", 100),
        new ProductRecord("2", "p2", "c2", 200)
    ];

    /// <summary>
    /// The seeded categories, in source order.
    /// </summary>
    public static readonly IReadOnlyList<CategoryRecord> Categories =
    [
        new CategoryRecord("c1", "t1", "1"),
        new CategoryRecord("c2", "t2", "2")
    ];
}

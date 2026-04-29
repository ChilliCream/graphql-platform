namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Link;

/// <summary>
/// Seed data for the <c>link</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/complex-entity-call/data.ts</c>.
/// </summary>
internal static class LinkData
{
    public static readonly IReadOnlyList<Product> Items =
    [
        new Product { Id = "1", Pid = "p1" },
        new Product { Id = "2", Pid = "p2" }
    ];

    public static readonly IReadOnlyDictionary<string, Product> ById =
        Items.ToDictionary(static p => p.Id, StringComparer.Ordinal);
}

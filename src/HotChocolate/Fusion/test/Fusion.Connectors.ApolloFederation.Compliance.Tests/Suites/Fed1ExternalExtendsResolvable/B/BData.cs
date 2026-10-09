namespace HotChocolate.Fusion.Suites.Fed1ExternalExtendsResolvable.B;

/// <summary>
/// Seed data for the <c>b</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/fed1-external-extends-resolvable/data.ts</c>.
/// </summary>
internal static class BData
{
    public static readonly IReadOnlyList<Product> Items =
    [
        new Product { Id = "p1", Name = "p1-name", Upc = "upc1", Price = 12.3 }
    ];
}

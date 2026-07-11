namespace HotChocolate.Fusion.Suites.Fed1ExternalExtendsResolvable.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/fed1-external-extends-resolvable/data.ts</c>.
/// </summary>
internal static class AData
{
    public static readonly IReadOnlyList<Product> Items =
    [
        new Product { Id = "p1", Name = "p1-name", Pid = "p1-pid" }
    ];

    public static readonly IReadOnlyDictionary<string, Product> ById =
        Items.ToDictionary(static p => p.Id, StringComparer.Ordinal);
}

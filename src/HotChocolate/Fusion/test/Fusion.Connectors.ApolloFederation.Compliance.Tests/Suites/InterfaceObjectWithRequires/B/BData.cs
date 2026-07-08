namespace HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.B;

/// <summary>
/// Seed data for the <c>b</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/interface-object-with-requires/data.ts</c>.
/// </summary>
internal static class BData
{
    public static readonly IReadOnlyList<string> Ids = ["u1", "u2"];

    public static readonly IReadOnlyDictionary<string, string> UsernamesById =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["u1"] = "u1-username",
            ["u2"] = "u2-username"
        };

    public static IReadOnlyList<NodeWithName> Nodes =>
        Ids.Select(static id => new NodeWithName { Id = id }).ToList();
}

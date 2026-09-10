namespace HotChocolate.Fusion.Suites.UnionInterfaceDistributed.B;

/// <summary>
/// Seed data for subgraph <c>b</c>. Oven warranty is hardcoded to 1.
/// </summary>
internal static class SubgraphBData
{
    public static readonly IReadOnlyDictionary<string, Oven> OvensById =
        new Dictionary<string, Oven>(StringComparer.Ordinal)
        {
            ["oven1"] = new Oven { Id = "oven1", Warranty = 1 },
            ["oven2"] = new Oven { Id = "oven2", Warranty = 1 }
        };
}

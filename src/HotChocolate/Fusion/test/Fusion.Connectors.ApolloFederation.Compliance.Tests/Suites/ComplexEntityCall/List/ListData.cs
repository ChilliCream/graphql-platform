namespace HotChocolate.Fusion.Suites.ComplexEntityCall.List;

/// <summary>
/// Seed data for the <c>list</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/complex-entity-call/data.ts</c>.
/// The list subgraph returns <c>first = products[0]</c> and
/// <c>selected = products[1]</c> for any product list it is asked about.
/// </summary>
internal static class ListData
{
    public static readonly IReadOnlyList<Product> Items =
    [
        new Product { Id = "1", Pid = "p1" },
        new Product { Id = "2", Pid = "p2" }
    ];

    public static readonly IReadOnlyDictionary<(string Id, string Pid), Product> ByIdAndPid =
        Items.ToDictionary(static p => (p.Id, p.Pid ?? string.Empty));

    public static Product ResolveIdPid(string id, string? pid)
    {
        if (pid is not null
            && ByIdAndPid.TryGetValue((id, pid), out var exact))
        {
            return exact;
        }

        return Items.FirstOrDefault(p => p.Id.Equals(id, StringComparison.Ordinal))
            ?? new Product { Id = id, Pid = pid };
    }
}

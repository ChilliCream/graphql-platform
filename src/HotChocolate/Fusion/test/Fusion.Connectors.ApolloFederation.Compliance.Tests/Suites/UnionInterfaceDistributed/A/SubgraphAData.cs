namespace HotChocolate.Fusion.Suites.UnionInterfaceDistributed.A;

/// <summary>
/// Seed data for subgraph <c>a</c>.
/// </summary>
internal static class SubgraphAData
{
    public static readonly IReadOnlyList<Oven> Ovens =
    [
        new Oven { Id = "oven1", Warranty = 1 },
        new Oven { Id = "oven2", Warranty = 2 }
    ];

    public static readonly IReadOnlyDictionary<string, Oven> OvensById =
        Ovens.ToDictionary(static o => o.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyList<Toaster> Toasters =
    [
        new Toaster { Id = "toaster1", Warranty = 3 },
        new Toaster { Id = "toaster2", Warranty = 4 }
    ];

    public static readonly IReadOnlyDictionary<string, Toaster> ToastersById =
        Toasters.ToDictionary(static t => t.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyList<IProduct> Products =
    [
        ..Ovens.Cast<IProduct>(),
        ..Toasters.Cast<IProduct>()
    ];
}

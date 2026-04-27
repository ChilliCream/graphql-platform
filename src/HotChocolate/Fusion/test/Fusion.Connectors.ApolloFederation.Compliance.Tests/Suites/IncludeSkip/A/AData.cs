namespace HotChocolate.Fusion.Suites.IncludeSkip.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph.
/// </summary>
internal static class AData
{
    public static readonly IReadOnlyList<Product> Products =
    [
        new Product { Id = "p1", Price = 699.99 }
    ];

    public static readonly IReadOnlyDictionary<string, Product> ById =
        Products.ToDictionary(static p => p.Id, StringComparer.Ordinal);
}

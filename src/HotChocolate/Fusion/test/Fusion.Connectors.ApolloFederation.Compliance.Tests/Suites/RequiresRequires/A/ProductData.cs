namespace HotChocolate.Fusion.Suites.RequiresRequires.A;

/// <summary>
/// Seed data for subgraph <c>a</c>.
/// </summary>
internal static class ProductData
{
    public static readonly IReadOnlyList<Product> Products =
    [
        new Product { Id = "p1", Price = 699.99 }
    ];

    public static readonly IReadOnlyDictionary<string, Product> ById =
        Products.ToDictionary(static p => p.Id, StringComparer.Ordinal);
}

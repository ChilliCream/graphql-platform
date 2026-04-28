namespace HotChocolate.Fusion.Suites.RequiresRequires.B;

/// <summary>
/// Seed data for subgraph <c>b</c>.
/// </summary>
internal static class ProductData
{
    public static readonly IReadOnlyList<Product> Products =
    [
        new Product { Id = "p1", HasDiscount = true }
    ];

    public static readonly IReadOnlyDictionary<string, Product> ById =
        Products.ToDictionary(static p => p.Id, StringComparer.Ordinal);
}

namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Products;

/// <summary>
/// Seed data for the <c>products</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-requires-provides/data.ts</c>.
/// </summary>
internal static class ProductsData
{
    /// <summary>
    /// The seeded <see cref="Product"/> entities, ordered by <c>upc</c>.
    /// </summary>
    public static readonly IReadOnlyList<Product> Products =
    [
        new Product { Upc = "p1", Name = "p-name-1", Price = 11, Weight = 1 },
        new Product { Upc = "p2", Name = "p-name-2", Price = 22, Weight = 2 }
    ];

    /// <summary>
    /// The seeded <see cref="Product"/> entities indexed by their <c>upc</c>
    /// field.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Product> ByUpc =
        Products.ToDictionary(static p => p.Upc, StringComparer.Ordinal);
}

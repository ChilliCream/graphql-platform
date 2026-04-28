namespace HotChocolate.Fusion.Suites.MysteriousExternal.Price;

/// <summary>
/// Seed data for the <c>price</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/mysterious-external/data.ts</c>.
/// </summary>
internal static class PriceData
{
    /// <summary>
    /// The seeded <see cref="Product"/> entities with pricing information.
    /// </summary>
    public static readonly IReadOnlyList<Product> Products =
    [
        new Product { Id = "1", Price = 100.0 },
        new Product { Id = "2", Price = 200.0 }
    ];

    /// <summary>
    /// The seeded <see cref="Product"/> entities indexed by their <c>id</c> field.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Product> ById =
        Products.ToDictionary(static p => p.Id, StringComparer.Ordinal);

    /// <summary>
    /// The product with the lowest price.
    /// </summary>
    public static readonly Product CheapestProduct =
        Products.OrderBy(static p => p.Price).First();
}

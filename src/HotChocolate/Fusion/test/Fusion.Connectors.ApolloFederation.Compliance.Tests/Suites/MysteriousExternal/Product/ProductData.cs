namespace HotChocolate.Fusion.Suites.MysteriousExternal.Product;

/// <summary>
/// Seed data for the <c>product</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/mysterious-external/data.ts</c>.
/// </summary>
internal static class ProductData
{
    /// <summary>
    /// The seeded <see cref="Product"/> entities, ordered by id.
    /// </summary>
    public static readonly IReadOnlyList<Product> Products =
    [
        new Product { Id = "1", Name = "name-1" },
        new Product { Id = "2", Name = "name-2" }
    ];

    /// <summary>
    /// The seeded <see cref="Product"/> entities indexed by their <c>id</c> field.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Product> ById =
        Products.ToDictionary(static p => p.Id, StringComparer.Ordinal);
}

namespace HotChocolate.Fusion.Suites.Node.Types;

/// <summary>
/// Seed data for the <c>types</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/node/data.ts</c>.
/// </summary>
internal static class TypesData
{
    public static readonly IReadOnlyDictionary<string, Product> ProductsById =
        new Dictionary<string, Product>(StringComparer.Ordinal)
        {
            ["p-1"] = new Product { Id = "p-1", Name = "Product 1", Price = 10.0 },
            ["p-2"] = new Product { Id = "p-2", Name = "Product 2", Price = 20.0 }
        };

    public static readonly IReadOnlyDictionary<string, Category> CategoriesById =
        new Dictionary<string, Category>(StringComparer.Ordinal)
        {
            ["pc-1"] = new Category { Id = "pc-1", Name = "Category 1" },
            ["c-2"] = new Category { Id = "c-2", Name = "Category 2" }
        };
}

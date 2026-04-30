namespace HotChocolate.Fusion.Suites.Node.Node;

/// <summary>
/// Seed data for the <c>node</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/node/data.ts</c>.
/// </summary>
internal static class NodeData
{
    public static readonly IReadOnlyList<Product> Products =
    [
        new Product { Id = "p-1" },
        new Product { Id = "p-2" }
    ];

    public static readonly IReadOnlyList<Category> Categories =
    [
        new Category { Id = "pc-1" },
        new Category { Id = "c-2" }
    ];

    public static readonly IReadOnlyDictionary<string, Product> ProductsById =
        Products.ToDictionary(static p => p.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyDictionary<string, Category> CategoriesById =
        Categories.ToDictionary(static c => c.Id, StringComparer.Ordinal);
}

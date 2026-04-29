namespace HotChocolate.Fusion.Suites.Node.NodeTwo;

/// <summary>
/// Seed data for the <c>node-two</c> subgraph.
/// </summary>
internal static class NodeTwoData
{
    public static readonly IReadOnlyDictionary<string, Product> ProductsById =
        new Dictionary<string, Product>(StringComparer.Ordinal)
        {
            ["p-1"] = new Product { Id = "p-1" },
            ["p-2"] = new Product { Id = "p-2" }
        };

    public static readonly IReadOnlyDictionary<string, Category> CategoriesById =
        new Dictionary<string, Category>(StringComparer.Ordinal)
        {
            ["pc-1"] = new Category { Id = "pc-1" },
            ["c-2"] = new Category { Id = "c-2" }
        };
}

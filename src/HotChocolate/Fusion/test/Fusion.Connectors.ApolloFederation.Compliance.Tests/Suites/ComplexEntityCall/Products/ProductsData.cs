namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Products;

/// <summary>
/// Seed data for the <c>products</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/complex-entity-call/data.ts</c>.
/// </summary>
internal static class ProductsData
{
    public static readonly IReadOnlyList<Product> Items =
    [
        new Product { Id = "1", CategoryId = "c1" },
        new Product { Id = "2", CategoryId = "c2" }
    ];

    public static readonly IReadOnlyDictionary<string, Product> ById =
        Items.ToDictionary(static p => p.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyList<Category> Categories =
    [
        new Category { Id = "c1", Tag = "t1", MainProductId = "1" },
        new Category { Id = "c2", Tag = "t2", MainProductId = "2" }
    ];

    public static readonly IReadOnlyDictionary<string, Category> CategoriesById =
        Categories.ToDictionary(static c => c.Id, StringComparer.Ordinal);
}

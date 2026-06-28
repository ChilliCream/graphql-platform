namespace HotChocolate.Fusion.Suites.SharedRoot.Category;

/// <summary>
/// Seed data for the <c>category</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/shared-root/data.ts</c>.
/// </summary>
internal static class CategoryData
{
    public static readonly Product Product = new()
    {
        Id = "1",
        Category = new Category { Id = "1", Name = "Category 1" }
    };
}

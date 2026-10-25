namespace HotChocolate.Fusion.Suites.NestedProvides.Subcategories;

/// <summary>
/// Seed data for the <c>subcategories</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/nested-provides/data.ts</c>.
/// </summary>
internal static class SubcategoriesData
{
    /// <summary>
    /// Product-to-category associations. Each product maps to an ordered
    /// list of category identifiers.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ProductCategories =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["p1"] = ["c1", "c2"],
            ["p2"] = ["c3", "c2"]
        };

    /// <summary>
    /// Category-to-subcategory associations. Each category maps to an
    /// ordered list of subcategory identifiers.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> SubCategories =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["c1"] = ["c2"],
            ["c2"] = ["c3"],
            ["c3"] = ["c1"]
        };

    /// <summary>
    /// Builds a fully resolved <see cref="Product"/> with nested
    /// <see cref="CategoryEntity"/> objects (categories with their
    /// subcategories).
    /// </summary>
    public static Product BuildProduct(string id)
    {
        if (!ProductCategories.TryGetValue(id, out var catIds))
        {
            return new Product { Id = id };
        }

        var categories = new List<CategoryEntity>(catIds.Count);

        foreach (var catId in catIds)
        {
            categories.Add(new CategoryEntity { Id = catId });
        }

        return new Product { Id = id, Categories = categories };
    }

    /// <summary>
    /// Builds a <see cref="CategoryEntity"/> reference.
    /// </summary>
    public static CategoryEntity BuildCategory(string id) => new() { Id = id };

    public static IReadOnlyList<CategoryEntity> GetSubCategories(string id)
        => SubCategories.TryGetValue(id, out var subIds)
            ? subIds.Select(static subId => new CategoryEntity { Id = subId }).ToArray()
            : [];
}

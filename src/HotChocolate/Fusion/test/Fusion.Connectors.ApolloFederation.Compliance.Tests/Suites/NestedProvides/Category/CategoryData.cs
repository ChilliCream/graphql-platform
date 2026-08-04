namespace HotChocolate.Fusion.Suites.NestedProvides.Category;

/// <summary>
/// Seed data for the <c>category</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/nested-provides/data.ts</c>.
/// </summary>
internal static class CategoryData
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
    /// Category seed entries indexed by <c>id</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, CategorySeed> Categories =
        new Dictionary<string, CategorySeed>(StringComparer.Ordinal)
        {
            ["c1"] = new("c1", "Category 1", ["c2"]),
            ["c2"] = new("c2", "Category 2", ["c3"]),
            ["c3"] = new("c3", "Category 3", ["c1"])
        };

    /// <summary>
    /// Builds a fully resolved <see cref="Product"/> with nested
    /// <see cref="CategoryEntity"/> objects (one level of subcategories).
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
            categories.Add(BuildCategory(catId));
        }

        return new Product { Id = id, Categories = categories };
    }

    /// <summary>
    /// Builds a <see cref="CategoryEntity"/> with its name and one level
    /// of resolved subcategories (each with id and name).
    /// </summary>
    public static CategoryEntity BuildCategory(string id)
    {
        if (!Categories.TryGetValue(id, out var seed))
        {
            return new CategoryEntity { Id = id };
        }

        List<CategoryEntity>? subs = null;

        if (seed.SubCategoryIds is { Count: > 0 })
        {
            subs = new List<CategoryEntity>(seed.SubCategoryIds.Count);

            foreach (var subId in seed.SubCategoryIds)
            {
                var subSeed = Categories.TryGetValue(subId, out var s) ? s : null;
                subs.Add(new CategoryEntity
                {
                    Id = subId,
                    Name = subSeed?.Name
                });
            }
        }

        return new CategoryEntity
        {
            Id = id,
            Name = seed.Name,
            SubCategories = subs
        };
    }

    /// <summary>
    /// Internal record used to hold raw seed values before building
    /// entity graphs.
    /// </summary>
    internal sealed record CategorySeed(
        string Id,
        string Name,
        IReadOnlyList<string> SubCategoryIds);
}

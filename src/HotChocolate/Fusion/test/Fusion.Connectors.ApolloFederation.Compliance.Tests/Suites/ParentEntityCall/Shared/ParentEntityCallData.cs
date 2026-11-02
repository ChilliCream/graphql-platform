namespace HotChocolate.Fusion.Suites.ParentEntityCall.Shared;

/// <summary>
/// Seed data for the <c>parent-entity-call</c> suite, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/parent-entity-call/data.ts</c>.
/// All three subgraphs (<c>a</c>, <c>b</c>, <c>c</c>) share the same product
/// and category fixtures so the gateway can validate cross-subgraph entity
/// joins against a single source of truth.
/// </summary>
public static class ParentEntityCallData
{
    /// <summary>
    /// The seeded product rows. Each carries the foreign key
    /// <see cref="ProductRow.CategoryId"/> that resolves into
    /// <see cref="Categories"/>.
    /// </summary>
    public static readonly IReadOnlyList<ProductRow> Products =
    [
        new ProductRow("p1", "p1-pid", "c1"),
        new ProductRow("p2", "p2-pid", "c2"),
        new ProductRow("p3", "p3-pid", "c1")
    ];

    /// <summary>
    /// The seeded category rows. The audit's <c>data.ts</c> derives
    /// <see cref="CategoryRow.DetailsProductsCount"/> as the number of
    /// products that reference the category.
    /// </summary>
    public static readonly IReadOnlyList<CategoryRow> Categories =
    [
        new CategoryRow(
            "c1",
            "c1-name",
            Products.Count(p => string.Equals(p.CategoryId, "c1", StringComparison.Ordinal))),
        new CategoryRow(
            "c2",
            "c2-name",
            Products.Count(p => string.Equals(p.CategoryId, "c2", StringComparison.Ordinal)))
    ];

    /// <summary>
    /// Looks up a product row by both <see cref="ProductRow.Id"/> and
    /// <see cref="ProductRow.Pid"/>. Returns <c>null</c> when no row matches.
    /// </summary>
    public static ProductRow? FindProduct(string id, string? pid)
    {
        foreach (var product in Products)
        {
            if (!string.Equals(product.Id, id, StringComparison.Ordinal))
            {
                continue;
            }

            if (pid is not null
                && !string.Equals(product.Pid, pid, StringComparison.Ordinal))
            {
                continue;
            }

            return product;
        }

        return null;
    }

    /// <summary>
    /// Looks up a category row by id, returning <c>null</c> when absent.
    /// </summary>
    public static CategoryRow? FindCategory(string id)
    {
        foreach (var category in Categories)
        {
            if (string.Equals(category.Id, id, StringComparison.Ordinal))
            {
                return category;
            }
        }

        return null;
    }
}

/// <summary>
/// Source row for a product in the <c>parent-entity-call</c> seed data.
/// Mirrors the audit's plain object literal one to one.
/// </summary>
public sealed record ProductRow(string Id, string Pid, string CategoryId);

/// <summary>
/// Source row for a category in the <c>parent-entity-call</c> seed data.
/// </summary>
public sealed record CategoryRow(string Id, string Name, int DetailsProductsCount);

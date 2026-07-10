namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Inventory;

/// <summary>
/// Seed data for the <c>inventory</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/simple-requires-provides/data.ts</c>.
/// Only <c>p1</c> is in stock; the seed list of product Universal Product
/// Codes (UPCs) is intentionally narrow.
/// </summary>
internal static class InventoryData
{
    /// <summary>
    /// The set of product Universal Product Codes (UPCs) that are in stock.
    /// </summary>
    public static readonly IReadOnlySet<string> InStock =
        new HashSet<string>(StringComparer.Ordinal) { "p1" };

    /// <summary>
    /// The known product Universal Product Codes (UPCs) the inventory
    /// subgraph recognizes when resolving entity references.
    /// </summary>
    public static readonly IReadOnlySet<string> KnownUpcs =
        new HashSet<string>(StringComparer.Ordinal) { "p1", "p2" };
}

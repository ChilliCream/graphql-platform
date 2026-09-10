namespace HotChocolate.Fusion.Suites.RequiresWithArgumentConflict.B;

/// <summary>
/// Seed data for the <c>b</c> subgraph, transcribed from the
/// <c>requires-with-argument-conflict</c> audit suite data source.
/// </summary>
internal static class BData
{
    public static readonly IReadOnlyList<Product> Products =
    [
        new Product
        {
            Upc = "p1",
            Name = "p-name-1",
            Price = 11,
            Weight = 1,
            Category = new Category { AveragePrice = 11 }
        },
        new Product
        {
            Upc = "p2",
            Name = "p-name-2",
            Price = 22,
            Weight = 2,
            Category = new Category { AveragePrice = 22 }
        }
    ];

    public static readonly IReadOnlyDictionary<string, Product> ByUpc =
        Products.ToDictionary(static p => p.Upc, StringComparer.Ordinal);
}

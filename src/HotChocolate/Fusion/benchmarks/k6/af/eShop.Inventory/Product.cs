using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

namespace eShop.Inventory;

[ExtendServiceType]
[Key("upc")]
public sealed class Product
{
    [External]
    public string Upc { get; set; } = default!;

    [External]
    public int Weight { get; set; }

    [External]
    public int Price { get; set; }

    public bool InStock { get; set; }

    [Requires("price weight")]
    public int? GetShippingEstimate() => Price > 1000 ? 0 : Weight / 2;

    private static readonly Dictionary<string, Product> s_inventory = new()
    {
        { "1", new Product { Upc = "1", InStock = true } },
        { "2", new Product { Upc = "2", InStock = false } },
        { "3", new Product { Upc = "3", InStock = false } },
        { "4", new Product { Upc = "4", InStock = false } },
        { "5", new Product { Upc = "5", InStock = true } },
        { "6", new Product { Upc = "6", InStock = true } },
        { "7", new Product { Upc = "7", InStock = true } },
        { "8", new Product { Upc = "8", InStock = false } },
        { "9", new Product { Upc = "9", InStock = true } }
    };

    [ReferenceResolver]
    public static Product? ResolveReference(string upc)
        => s_inventory.TryGetValue(upc, out var product) ? product : null;
}

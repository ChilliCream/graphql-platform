using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;

namespace eShop.Inventory;

[QueryType]
public static partial class ProductQueries
{
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

    [Lookup, Internal]
    public static Product? GetProductByUpc([ID] string upc)
    {
        s_inventory.TryGetValue(upc, out var product);
        return product;
    }
}

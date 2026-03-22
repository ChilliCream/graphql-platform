using HotChocolate.Types;
using HotChocolate.Types.Relay;
using HotChocolate.Types.Composite;

namespace eShop.Products;

[QueryType]
public static partial class ProductQueries
{
    private static readonly List<Product> s_products =
    [
        new() { Upc = "1", Name = "Table", Price = 899, Weight = 100 },
        new() { Upc = "2", Name = "Couch", Price = 1299, Weight = 1000 },
        new() { Upc = "3", Name = "Glass", Price = 15, Weight = 20 },
        new() { Upc = "4", Name = "Chair", Price = 499, Weight = 100 },
        new() { Upc = "5", Name = "TV", Price = 1299, Weight = 1000 },
        new() { Upc = "6", Name = "Lamp", Price = 6999, Weight = 300 },
        new() { Upc = "7", Name = "Grill", Price = 3999, Weight = 2000 },
        new() { Upc = "8", Name = "Fridge", Price = 100000, Weight = 6000 },
        new() { Upc = "9", Name = "Sofa", Price = 9999, Weight = 800 }
    ];

    public static IEnumerable<Product> GetTopProducts(int first = 5)
        => s_products.Take(first);

    [Lookup]
    public static Product? GetProduct([ID] string upc)
        => s_products.FirstOrDefault(p => p.Upc == upc);
}

using HotChocolate.Types;
using HotChocolate.Types.Relay;
using HotChocolate.Types.Composite;

namespace eShop.Products;

[QueryType]
public static partial class ProductQueries
{
    private static readonly List<Product> s_products =
    [
        new() { Upc = "1", Name = "Glass", Price = 1500, Weight = 100 },
        new() { Upc = "2", Name = "Pen", Price = 200, Weight = 25 },
        new() { Upc = "3", Name = "Notebook", Price = 500, Weight = 400 },
        new() { Upc = "4", Name = "Bag", Price = 2500, Weight = 1000 },
        new() { Upc = "5", Name = "Lamp", Price = 3500, Weight = 500 },
        new() { Upc = "6", Name = "Chair", Price = 15000, Weight = 7500 },
        new() { Upc = "7", Name = "Desk", Price = 35000, Weight = 15000 },
        new() { Upc = "8", Name = "Monitor", Price = 50000, Weight = 5000 },
        new() { Upc = "9", Name = "Fridge", Price = 100000, Weight = 50000 }
    ];

    public static IEnumerable<Product> GetTopProducts(int first = 5)
        => s_products.Take(first);

    [Lookup]
    public static Product? GetProduct([ID] string upc)
        => s_products.FirstOrDefault(p => p.Upc == upc);
}

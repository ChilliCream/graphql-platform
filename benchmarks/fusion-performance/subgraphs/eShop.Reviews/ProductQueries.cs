using HotChocolate.Types;
using HotChocolate.Types.Relay;
using HotChocolate.Types.Composite;

namespace eShop.Reviews;

[QueryType]
public static partial class ProductQueries
{
    internal static readonly Dictionary<string, Product> Products =
        new(StringComparer.Ordinal)
    {
        ["1"] = new() { Upc = "1" },
        ["2"] = new() { Upc = "2" },
        ["3"] = new() { Upc = "3" },
        ["4"] = new() { Upc = "4" },
        ["5"] = new() { Upc = "5" },
        ["6"] = new() { Upc = "6" },
        ["7"] = new() { Upc = "7" },
        ["8"] = new() { Upc = "8" },
        ["9"] = new() { Upc = "9" }
    };

    [Lookup, Internal]
    public static Product? GetProduct([ID] string upc)
        => Products.TryGetValue(upc, out var product) ? product : null;
}

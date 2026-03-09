using HotChocolate.Types;
using HotChocolate.Types.Relay;
using HotChocolate.Types.Composite;

namespace eShop.Reviews;

[QueryType]
public static partial class ProductQueries
{
    [Lookup, Internal]
    public static Product GetProduct([ID] string upc)
        => new() { Upc = upc };
}

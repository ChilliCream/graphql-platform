
using HotChocolate;
using HotChocolate.Types;

namespace eShop.Inventory;

[ObjectType<Product>]
public static partial class ProductNode
{
    public static long? GetShippingEstimate([Parent] Product product, long weight, long price)
        => price > 1000 ? 0 : weight / 2;
}

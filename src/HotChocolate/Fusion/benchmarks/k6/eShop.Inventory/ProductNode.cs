
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Composite;

namespace eShop.Inventory;

[ObjectType<Product>]
public static partial class ProductNode
{
    public static long? GetShippingEstimate(
        [Parent] Product product,
        [Require] long weight,
        [Require] long price)
        => price > 1000 ? 0 : weight / 2;
}

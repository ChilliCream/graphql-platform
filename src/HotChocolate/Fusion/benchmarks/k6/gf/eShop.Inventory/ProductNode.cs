using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Composite;

namespace eShop.Inventory;

[ObjectType<Product>]
public static partial class ProductNode
{
#pragma warning disable RCS1163
    public static long? GetShippingEstimate(
        [Parent] Product product,
        [Require] long weight,
        [Require] long price)
#pragma warning restore RCS1163
        => price > 1000 ? 0 : weight / 2;
}

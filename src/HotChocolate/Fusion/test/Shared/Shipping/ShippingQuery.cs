namespace HotChocolate.Fusion.Shared.Shipping;

public sealed class ShippingQuery
{
    public ProductDimension GetProductDimensionByProductId(int productId)
        => new(productId, 15, 20);
}

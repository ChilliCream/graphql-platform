namespace HotChocolate.Fusion.Shared.Shipping;

[GraphQLName("Query")]
public sealed class ShippingQuery
{
    public ProductDimension GetProductDimensionByProductId(int productId)
        => new(productId, 15, 20);
}

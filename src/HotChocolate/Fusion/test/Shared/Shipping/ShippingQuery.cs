using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Shipping;

[GraphQLName("Query")]
public sealed class ShippingQuery
{
    public Product GetProductById([ID<Product>] int id)
        => new(id);
}

public sealed record Product([property: ID<Product>] int Id)
{
    public DeliveryEstimate GetDeliveryEstimate(string zip, int weight, int size)
        => new(1 * (weight + size), 2 * (weight + size));
}

using HotChocolate.Fusion.SourceSchema.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Shipping2;

[GraphQLName("Query")]
public sealed class ShippingQuery
{
    [Lookup]
    [Internal]
    public Product GetProductById(
        [Is("id")]
        [ID<Product>]
        int id)
        => new(id);
}

public sealed record Product([property: ID<Product>] int Id)
{
    public DeliveryEstimate GetDeliveryEstimate(
        string zip,
        [Require("dimension { weight }")] int weight,
        [Require("dimension { size }")] int size)
        => new(1 * (weight + size), 2 * (weight + size));
}

using HotChocolate.Execution;
using HotChocolate.Fusion.SourceSchema.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public static class SourceSchemaTests
{
    [Fact]
    public static async Task SourceSchemaSnapshot()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<ShippingQuery>()
                .AddGlobalObjectIdentification(registerNodeInterface: false)
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

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

    public sealed record DeliveryEstimate(int Min, int Max);
}

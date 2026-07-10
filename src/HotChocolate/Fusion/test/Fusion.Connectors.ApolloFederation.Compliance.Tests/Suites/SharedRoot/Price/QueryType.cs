using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SharedRoot.Price;

/// <summary>
/// Root <c>Query</c> for the <c>price</c> subgraph. Exposes
/// <c>product: Product!</c> and <c>products: [Product!]!</c>, both
/// shareable, returning the seeded price-only projection.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("product")
            .Type<NonNullType<ProductType>>()
            .Shareable()
            .Resolve(_ => PriceData.Product);

        descriptor
            .Field("products")
            .Type<NonNullType<ListType<NonNullType<ProductType>>>>()
            .Shareable()
            .Resolve(_ => new[] { PriceData.Product });
    }
}

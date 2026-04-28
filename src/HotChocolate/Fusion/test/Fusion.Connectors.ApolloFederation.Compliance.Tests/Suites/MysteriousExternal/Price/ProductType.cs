using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.MysteriousExternal.Price;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity as seen by
/// the <c>price</c> subgraph. Declares <c>@key(fields: "id")</c> and owns
/// <c>price: Float</c>. The original SDL uses <c>extend type</c> with
/// <c>id @external</c>, but HotChocolate Fusion composition requires the
/// key field to be accessible (not external) for cross-subgraph lookups.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Price).Type<FloatType>();
    }

    private static Product? ResolveById(string id)
        => PriceData.ById.TryGetValue(id, out var product) ? product : null;
}

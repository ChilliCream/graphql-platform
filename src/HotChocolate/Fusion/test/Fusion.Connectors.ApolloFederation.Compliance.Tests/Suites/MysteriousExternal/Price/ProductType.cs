using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.MysteriousExternal.Price;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity as seen by
/// the <c>price</c> subgraph. Extends <c>Product</c>, declares
/// <c>@key(fields: "id")</c>, and owns <c>price: Float</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .ExtendServiceType()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).External().Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Price).Type<FloatType>();
    }

    private static Product? ResolveById(string id)
        => PriceData.ById.TryGetValue(id, out var product) ? product : null;
}

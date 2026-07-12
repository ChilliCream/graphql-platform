using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.MysteriousExternal.Product;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity owned by the
/// <c>product</c> subgraph. Declares <c>@key(fields: "id")</c> with fields
/// <c>id: ID!</c> and <c>name: String!</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Name).Type<NonNullType<StringType>>();
    }

    private static Product? ResolveById(string id)
        => ProductData.ById.TryGetValue(id, out var product) ? product : null;
}

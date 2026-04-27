using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NestedProvides.AllProducts;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity owned by
/// the <c>all-products</c> subgraph. Mirrors the audit SDL
/// <c>type Product @key(fields: "id")</c> with only the <c>id: ID!</c>
/// field.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
    }

    private static Product ResolveById(string id) => new() { Id = id };
}

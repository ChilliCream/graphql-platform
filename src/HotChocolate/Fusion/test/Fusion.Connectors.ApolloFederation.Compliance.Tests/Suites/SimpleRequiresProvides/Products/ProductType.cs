using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Products;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity owned by the
/// <c>products</c> subgraph. Mirrors the audit Schema Definition Language
/// (SDL) <c>type Product @key(fields: "upc")</c> with fields
/// <c>upc: String!</c>, <c>name: String</c>, <c>price: Int</c>, and <c>weight: Int</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("upc")
            .ResolveReferenceWith(_ => ResolveByUpc(default!));

        descriptor.Field(p => p.Upc).Type<NonNullType<StringType>>();
        descriptor.Field(p => p.Name).Type<StringType>();
        descriptor.Field(p => p.Price).Type<IntType>();
        descriptor.Field(p => p.Weight).Type<IntType>();
    }

    private static Product? ResolveByUpc(string upc)
        => ProductsData.ByUpc.TryGetValue(upc, out var product) ? product : null;
}

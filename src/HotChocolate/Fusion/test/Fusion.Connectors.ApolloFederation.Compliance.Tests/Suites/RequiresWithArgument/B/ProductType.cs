using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity owned
/// by the <c>b</c> subgraph. Exposes <c>upc</c>, <c>name</c>,
/// <c>price(currency: String!)</c>, <c>weight</c>, and <c>category</c>.
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
        descriptor.Field(p => p.Weight).Type<IntType>();

        descriptor
            .Field("price")
            .Type<IntType>()
            .Argument("currency", a => a.Type<NonNullType<StringType>>())
            .Resolve(ctx => ctx.Parent<Product>().Price);

        descriptor.Field(p => p.Category).Type<CategoryType>();
    }

    private static Product? ResolveByUpc(string upc)
        => BData.ByUpc.TryGetValue(upc, out var product) ? product : null;
}

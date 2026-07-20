using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Reviews;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity as extended
/// by the <c>reviews</c> subgraph (<c>@key(fields: "upc")</c>). Owns
/// the <c>reviews</c> field; <c>name</c>, <c>price</c>, and <c>weight</c>
/// are owned elsewhere.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("upc")
            .ResolveReferenceWith(_ => ResolveByUpc(default!));

        descriptor.Field(p => p.Upc).Type<NonNullType<StringType>>();

        descriptor
            .Field("reviews")
            .Type<ListType<ReviewType>>()
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                return ReviewsData.Reviews
                    .Where(r => string.Equals(r.ProductUpc, product.Upc, StringComparison.Ordinal))
                    .ToArray();
            });
    }

    private static Product ResolveByUpc(string upc) => new() { Upc = upc };
}

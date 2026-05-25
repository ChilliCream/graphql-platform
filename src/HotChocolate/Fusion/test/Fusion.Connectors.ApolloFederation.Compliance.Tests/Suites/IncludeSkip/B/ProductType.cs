using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.IncludeSkip.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>b</c> subgraph (<c>@key(fields: "id")</c>). Owns
/// <c>isExpensive</c> via <c>@requires(fields: "price")</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Price).External().Type<NonNullType<FloatType>>();

        descriptor
            .Field("isExpensive")
            .Type<NonNullType<BooleanType>>()
            .Requires("price")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.Price is not double price)
                {
                    throw new InvalidOperationException("Price is missing.");
                }
                return price > 500d;
            });
    }

    private static Product ResolveById(string id) => new() { Id = id };
}

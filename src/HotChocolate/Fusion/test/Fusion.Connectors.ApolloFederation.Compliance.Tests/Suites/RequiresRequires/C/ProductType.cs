using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresRequires.C;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in
/// subgraph <c>c</c>. <c>price</c> and <c>hasDiscount</c> are external;
/// <c>isExpensive</c> requires <c>price</c> and
/// <c>isExpensiveWithDiscount</c> requires <c>hasDiscount</c>.
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
        descriptor.Field(p => p.HasDiscount).External().Type<NonNullType<BooleanType>>();

        descriptor
            .Field("isExpensive")
            .Type<NonNullType<BooleanType>>()
            .Requires("price")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.Price is not double price)
                {
                    throw new InvalidOperationException(
                        "isExpensive requires price on the parent entity.");
                }
                return price > 500;
            });

        descriptor
            .Field("isExpensiveWithDiscount")
            .Type<NonNullType<BooleanType>>()
            .Requires("hasDiscount")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.HasDiscount is not bool hasDiscount)
                {
                    throw new InvalidOperationException(
                        "isExpensiveWithDiscount requires hasDiscount on the parent entity.");
                }
                return !hasDiscount;
            });
    }

    private static Product? ResolveById(string id)
        => string.Equals(id, "p1", StringComparison.Ordinal)
            ? new Product { Id = id }
            : null;
}

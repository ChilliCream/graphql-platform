using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresRequires.D;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in
/// subgraph <c>d</c>. <c>isExpensive</c> and <c>isExpensiveWithDiscount</c>
/// are external; <c>canAfford</c> requires <c>isExpensive</c> and
/// <c>canAffordWithDiscount</c> requires <c>isExpensiveWithDiscount</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.IsExpensive).External().Type<NonNullType<BooleanType>>();
        descriptor.Field(p => p.IsExpensiveWithDiscount).External().Type<NonNullType<BooleanType>>();

        descriptor
            .Field("canAfford")
            .Type<NonNullType<BooleanType>>()
            .Requires("isExpensive")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.IsExpensive is not bool isExpensive)
                {
                    throw new InvalidOperationException(
                        "canAfford requires isExpensive on the parent entity.");
                }
                return !isExpensive;
            });

        descriptor
            .Field("canAffordWithDiscount")
            .Type<NonNullType<BooleanType>>()
            .Requires("isExpensiveWithDiscount")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.IsExpensiveWithDiscount is not bool isExpensiveWithDiscount)
                {
                    throw new InvalidOperationException(
                        "canAffordWithDiscount requires isExpensiveWithDiscount on the parent entity.");
                }
                return !isExpensiveWithDiscount;
            });
    }

    private static Product? ResolveById(string id)
        => string.Equals(id, "p1", StringComparison.Ordinal)
            ? new Product { Id = id }
            : null;
}

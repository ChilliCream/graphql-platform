using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Inventory;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>inventory</c> subgraph. Mirrors the audit Schema Definition Language
/// (SDL): owns <c>inStock</c>, <c>shippingEstimate</c>, and
/// <c>shippingEstimateTag</c>. The latter two require the external
/// <c>price</c> and <c>weight</c> fields, which the federation external
/// setter copies from the inbound entity representation onto the parent
/// before the resolver runs.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("upc")
            .ResolveReferenceWith(_ => ResolveByUpc(default!));

        descriptor.Field(p => p.Upc).Type<NonNullType<StringType>>();
        descriptor.Field(p => p.Weight).External().Type<IntType>();
        descriptor.Field(p => p.Price).External().Type<IntType>();

        descriptor
            .Field("inStock")
            .Type<BooleanType>()
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                return InventoryData.InStock.Contains(product.Upc);
            });

        descriptor
            .Field("shippingEstimate")
            .Type<IntType>()
            .Requires("price weight")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.Price is not int price || product.Weight is not int weight)
                {
                    throw new InvalidOperationException(
                        "shippingEstimate requires price and weight on the parent entity.");
                }
                return price * weight * 10;
            });

        descriptor
            .Field("shippingEstimateTag")
            .Type<StringType>()
            .Requires("price weight")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.Price is not int price || product.Weight is not int weight)
                {
                    throw new InvalidOperationException(
                        "shippingEstimateTag requires price and weight on the parent entity.");
                }
                return $"#{product.Upc}#{price * weight * 10}#";
            });
    }

    private static Product? ResolveByUpc(string upc)
        => InventoryData.KnownUpcs.Contains(upc)
            ? new Product { Upc = upc }
            : null;
}

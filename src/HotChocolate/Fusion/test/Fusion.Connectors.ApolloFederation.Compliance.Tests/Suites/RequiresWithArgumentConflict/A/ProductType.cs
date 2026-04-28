using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgumentConflict.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>a</c> subgraph. Owns <c>shippingEstimate</c> (requires
/// <c>price(currency: "USD")</c>), <c>shippingEstimateEUR</c>
/// (requires <c>price(currency: "EUR")</c>), and
/// <c>isExpensiveCategory</c> (requires
/// <c>category { averagePrice(currency: "USD") }</c>).
/// The two shipping estimates demonstrate conflicting argument values
/// on the same required field.
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

        descriptor.Field(p => p.Price)
            .External()
            .Type<IntType>()
            .Argument("currency", a => a.Type<NonNullType<StringType>>());

        descriptor.Field(p => p.Category).External().Type<CategoryType>();

        descriptor
            .Field("shippingEstimate")
            .Type<IntType>()
            .Requires("""price(currency: "USD") weight""")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.Price is not int price || product.Weight is not int weight)
                {
                    return null;
                }

                return price * weight * 10;
            });

        descriptor
            .Field("shippingEstimateEUR")
            .Type<IntType>()
            .Requires("""price(currency: "EUR") weight""")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.Price is not int price || product.Weight is not int weight)
                {
                    return null;
                }

                return price * weight * 10;
            });

        descriptor
            .Field("isExpensiveCategory")
            .Type<BooleanType>()
            .Requires("""category { averagePrice(currency: "USD") }""")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                if (product.Category?.AveragePrice is not int avgPrice)
                {
                    return null;
                }

                return avgPrice > 11;
            });
    }

    private static Product ResolveByUpc(string upc)
        => new() { Upc = upc };
}

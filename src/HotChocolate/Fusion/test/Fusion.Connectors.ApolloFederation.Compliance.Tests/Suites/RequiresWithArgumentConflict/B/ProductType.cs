using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgumentConflict.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity owned
/// by the <c>b</c> subgraph. The <c>price</c> field applies currency
/// conversion: EUR doubles the base price, USD returns it as-is.
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
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                var currency = ctx.ArgumentValue<string>("currency");

                return currency switch
                {
                    "EUR" => product.Price * 2,
                    "USD" => product.Price,
                    _ => throw new InvalidOperationException(
                        $"Unsupported currency {currency}")
                };
            });

        descriptor.Field(p => p.Category).Type<CategoryType>();
    }

    private static Product? ResolveByUpc(string upc)
        => BData.ByUpc.TryGetValue(upc, out var product) ? product : null;
}

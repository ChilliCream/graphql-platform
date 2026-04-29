using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Price;

/// <summary>
/// GraphQL type for the <c>Price</c> value returned by <c>Product.price</c>.
/// </summary>
public sealed class PriceType : ObjectType<Price>
{
    protected override void Configure(IObjectTypeDescriptor<Price> descriptor)
    {
        descriptor.Name("Price");
        descriptor.Field("price")
            .Type<NonNullType<FloatType>>()
            .Resolve(ctx => ctx.Parent<Price>().Value);

        descriptor.Ignore(p => p.Value);
    }
}

using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SharedRoot.Price;

/// <summary>
/// Descriptor for the <c>Price</c> value type owned by the <c>price</c>
/// subgraph.
/// </summary>
public sealed class PriceType : ObjectType<Price>
{
    protected override void Configure(IObjectTypeDescriptor<Price> descriptor)
    {
        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Amount).Type<NonNullType<IntType>>();
        descriptor.Field(p => p.Currency).Type<NonNullType<StringType>>();
    }
}

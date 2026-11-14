using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionOverfetching.Card;

public sealed class ProductCardType : ObjectType<ProductCard>
{
    protected override void Configure(IObjectTypeDescriptor<ProductCard> descriptor)
    {
        descriptor.Key("id");

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
        descriptor.Field(c => c.Components).Type<ListType<ComponentUnionType>>();
    }
}

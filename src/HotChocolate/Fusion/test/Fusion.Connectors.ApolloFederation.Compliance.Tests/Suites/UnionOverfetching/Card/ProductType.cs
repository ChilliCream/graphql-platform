using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionOverfetching.Card;

// Key-only entity: the "card" subgraph exposes no fields beyond the key.
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Key("id");

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
    }
}

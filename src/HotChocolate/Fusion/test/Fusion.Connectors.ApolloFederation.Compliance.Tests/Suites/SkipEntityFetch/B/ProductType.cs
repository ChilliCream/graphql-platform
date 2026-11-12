using HotChocolate.ApolloFederation.Types;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SkipEntityFetch.B;

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveReference(default!, default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Details).Type<ProductDetailsType>();
        descriptor.Field(p => p.Inventory).Type<ProductInventoryType>();
    }

    // A fetch whose selections are all skipped is one the gateway should never have made.
    private static Product? ResolveReference(string id, IResolverContext context)
    {
        if (!EntitySelection.SelectsAnyField(context))
        {
            throw new GraphQLException("Received an entity fetch where every field is skipped");
        }

        return string.Equals(id, BData.Product.Id, StringComparison.Ordinal)
            ? BData.Product
            : null;
    }
}

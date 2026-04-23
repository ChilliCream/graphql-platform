using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SharedRoot.Category;

/// <summary>
/// Root <c>Query</c> for the <c>category</c> subgraph. Exposes
/// <c>product: Product!</c> and <c>products: [Product!]!</c>, both
/// shareable, returning the seeded category-only projection.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("product")
            .Type<NonNullType<ProductType>>()
            .Shareable()
            .Resolve(_ => CategoryData.Product);

        descriptor
            .Field("products")
            .Type<NonNullType<ListType<NonNullType<ProductType>>>>()
            .Shareable()
            .Resolve(_ => new[] { CategoryData.Product });
    }
}

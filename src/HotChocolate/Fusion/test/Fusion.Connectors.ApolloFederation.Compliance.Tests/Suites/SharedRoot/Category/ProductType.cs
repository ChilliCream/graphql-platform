using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SharedRoot.Category;

/// <summary>
/// Descriptor for the <c>Product</c> contribution from the <c>category</c>
/// subgraph. The audit SDL declares no <c>@key</c>; the type is shared across
/// subgraphs through the shareable root <c>Query.product</c> /
/// <c>Query.products</c> only. Field <c>id</c> is shareable so any subgraph
/// can produce it.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Field(p => p.Id).Shareable().Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Category).Type<NonNullType<CategoryType>>();
    }
}

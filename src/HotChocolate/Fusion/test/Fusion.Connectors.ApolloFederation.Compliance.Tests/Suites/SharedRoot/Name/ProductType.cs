using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SharedRoot.Name;

/// <summary>
/// Descriptor for the <c>Product</c> contribution from the <c>name</c>
/// subgraph. Field <c>id</c> is shareable so any subgraph can produce it.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Field(p => p.Id).Shareable().Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Name).Type<NonNullType<NameType>>();
    }
}

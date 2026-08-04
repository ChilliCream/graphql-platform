using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Node.NodeTwo;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>node-two</c> subgraph (<c>@key(fields: "id")</c>).
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Implements<NodeType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
    }

    private static Product? ResolveById(string id)
        => NodeTwoData.ProductsById.TryGetValue(id, out var p) ? p : null;
}

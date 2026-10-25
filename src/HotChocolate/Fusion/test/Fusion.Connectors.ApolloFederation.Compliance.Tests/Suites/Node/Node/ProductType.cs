using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Node.Node;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>node</c> subgraph (<c>@key(fields: "id")</c>). Carries only the key
/// field; <c>name</c> and <c>price</c> are owned by the <c>types</c>
/// subgraph.
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
        => NodeData.ProductsById.TryGetValue(id, out var p) ? p : null;
}

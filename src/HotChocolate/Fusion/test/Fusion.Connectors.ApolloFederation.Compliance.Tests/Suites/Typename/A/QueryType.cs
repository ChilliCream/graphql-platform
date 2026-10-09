using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// Root <c>Query</c> for the <c>a</c> subgraph. Exposes <c>union: Product</c>
/// and <c>interface: Node</c> root fields. The audit's resolvers return an
/// <see cref="Oven"/> with id <c>"1"</c> for <c>union</c> and a
/// <see cref="Toaster"/> with id <c>"2"</c> for <c>interface</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("union")
            .Type<ProductUnionType>()
            .Resolve(_ => (object)new Oven { Id = "1" });

        descriptor
            .Field("interface")
            .Type<NodeType>()
            .Resolve(_ => (INode)new Toaster { Id = "2" });
    }
}

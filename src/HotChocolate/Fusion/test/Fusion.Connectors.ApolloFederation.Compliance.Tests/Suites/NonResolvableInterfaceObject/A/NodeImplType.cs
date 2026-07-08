using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.A;

public sealed class NodeImplType : ObjectType<NodeImpl>
{
    protected override void Configure(IObjectTypeDescriptor<NodeImpl> descriptor)
    {
        descriptor.Name("NodeImpl");

        descriptor.Implements<NodeType>();

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(n => n.Id).Type<NonNullType<IdType>>();
    }

    // The audit's Node.__resolveReference throws because the interface object is
    // declared non-resolvable in this subgraph; modelled here as an unresolvable
    // reference.
    private static NodeImpl? ResolveById(string id) => null;
}

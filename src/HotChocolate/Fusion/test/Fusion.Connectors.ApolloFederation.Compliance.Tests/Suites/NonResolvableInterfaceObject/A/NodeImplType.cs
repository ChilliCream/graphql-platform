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

    private static NodeImpl? ResolveById(string id)
        => throw new InvalidOperationException(
            "Not resolvable as it is an interface object.");
}

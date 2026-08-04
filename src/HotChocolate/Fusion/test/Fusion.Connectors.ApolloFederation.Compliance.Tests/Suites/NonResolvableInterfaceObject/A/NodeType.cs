using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.A;

public sealed class NodeType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Node");

        descriptor.Key("id");

        descriptor.Field("id").Type<NonNullType<IdType>>();
    }
}

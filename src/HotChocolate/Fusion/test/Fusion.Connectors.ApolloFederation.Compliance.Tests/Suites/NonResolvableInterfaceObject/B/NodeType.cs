using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.B;

public sealed class NodeType : ObjectType<Node>
{
    protected override void Configure(IObjectTypeDescriptor<Node> descriptor)
    {
        descriptor.InterfaceObject();

        descriptor.Key("id", resolvable: false);

        descriptor.Field(n => n.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field("field")
            .Type<StringType>()
            .Resolve(_ => "foo");
    }
}

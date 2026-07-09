using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.B;

/// <summary>
/// Apollo Federation descriptor for the <c>NodeWithName</c> interface object in
/// the <c>b</c> subgraph
/// (<c>type NodeWithName @key(fields: "id") @interfaceObject { id, username }</c>).
/// </summary>
public sealed class NodeWithNameType : ObjectType<NodeWithName>
{
    protected override void Configure(IObjectTypeDescriptor<NodeWithName> descriptor)
    {
        descriptor
            .InterfaceObject()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(n => n.Id).Type<NonNullType<IdType>>();
        descriptor.Field(n => n.Username).Type<StringType>();
    }

    private static NodeWithName ResolveById(string id)
        => new() { Id = id, Username = BData.FindUsername(id) };
}

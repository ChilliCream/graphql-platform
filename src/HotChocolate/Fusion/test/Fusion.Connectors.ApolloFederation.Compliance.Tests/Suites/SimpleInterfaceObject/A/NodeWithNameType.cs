using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;

/// <summary>
/// Apollo Federation descriptor for the <c>NodeWithName</c> interface in the
/// <c>a</c> subgraph
/// (<c>interface NodeWithName @key(fields: "id") { id: ID!, name: String }</c>).
/// </summary>
public sealed class NodeWithNameType : InterfaceType<INodeWithName>
{
    protected override void Configure(IInterfaceTypeDescriptor<INodeWithName> descriptor)
    {
        descriptor.Name("NodeWithName");
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(n => n.Id).Type<NonNullType<IdType>>();
        descriptor.Field(n => n.Name).Type<StringType>();
    }

    private static INodeWithName? ResolveById(string id) => AData.FindUser(id);
}

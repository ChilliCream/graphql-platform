using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.A;

/// <summary>
/// The federated <c>NodeWithName</c> interface owned by the <c>a</c> subgraph
/// (<c>@key(fields: "id")</c>). The reference resolver dispatches to the concrete
/// <c>User</c> implementer so the gateway can recover the concrete
/// <c>__typename</c> for the <c>NodeWithName @interfaceObject</c> declared in
/// subgraph <c>b</c>.
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

    private static INodeWithName? ResolveById(string id)
        => AData.ById.TryGetValue(id, out var user) ? user : null;
}

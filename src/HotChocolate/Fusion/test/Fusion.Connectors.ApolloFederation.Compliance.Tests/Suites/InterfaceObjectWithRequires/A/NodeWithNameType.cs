using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.A;

/// <summary>
/// The federated <c>NodeWithName</c> interface owned by the <c>a</c> subgraph
/// (<c>@key(fields: "id")</c>). Concrete implementations such as <c>User</c>
/// carry the entity reference resolver.
/// </summary>
public sealed class NodeWithNameType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("NodeWithName");
        descriptor.Key("id");

        descriptor.Field("id").Type<NonNullType<IdType>>();
        descriptor.Field("name").Type<StringType>();
    }
}

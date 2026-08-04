using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;

/// <summary>
/// Root <c>Query</c> for the <c>a</c> subgraph. Exposes
/// <c>users: [NodeWithName!]!</c> returning the seeded <c>User</c> entities.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("users")
            .Type<NonNullType<ListType<NonNullType<NodeWithNameType>>>>()
            .Resolve(_ => AData.Users);
    }
}

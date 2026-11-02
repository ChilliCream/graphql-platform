using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.A;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity in the <c>a</c>
/// subgraph. Implements <c>NodeWithName</c> and resolves entity references by
/// <c>id</c> (<c>@key(fields: "id")</c> + <c>__resolveReference</c>).
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Name("User");
        descriptor.Implements<NodeWithNameType>();

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
        descriptor.Field(u => u.Name).Type<StringType>();
        descriptor.Field(u => u.Age).Type<IntType>();
    }

    private static User? ResolveById(string id)
        => AData.ById.TryGetValue(id, out var user) ? user : null;
}

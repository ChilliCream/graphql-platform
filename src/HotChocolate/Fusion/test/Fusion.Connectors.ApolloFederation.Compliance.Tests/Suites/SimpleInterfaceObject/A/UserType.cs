using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity in the
/// <c>a</c> subgraph
/// (<c>type User implements NodeWithName @key(fields: "id") { id, name, age }</c>).
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Implements<NodeWithNameType>();
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
        descriptor.Field(u => u.Name).Type<StringType>();
        descriptor.Field(u => u.Age).Type<IntType>();
    }

    private static User? ResolveById(string id) => AData.FindUser(id);
}

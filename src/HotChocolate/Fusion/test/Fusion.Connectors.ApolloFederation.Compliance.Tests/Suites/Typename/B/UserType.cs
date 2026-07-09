using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.Typename.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Typename.B;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity in the
/// <c>b</c> subgraph. Mirrors the audit Schema Definition Language (SDL):
/// <c>type User @key(fields: "id") @interfaceObject { id: ID!, name: String! }</c>.
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .InterfaceObject()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
        descriptor.Field(u => u.Name).Type<NonNullType<StringType>>();
    }

    private static User? ResolveById(string id)
    {
        var row = TypenameData.FindUser(id);
        return row is null ? null : new User { Id = row.Id, Name = row.Name };
    }
}

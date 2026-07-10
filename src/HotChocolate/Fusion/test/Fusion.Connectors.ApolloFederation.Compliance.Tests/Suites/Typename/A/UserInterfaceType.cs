using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.Typename.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> interface in the
/// <c>a</c> subgraph
/// (<c>interface User @key(fields: "id") { id: ID! }</c>).
/// </summary>
public sealed class UserInterfaceType : InterfaceType<IUser>
{
    protected override void Configure(IInterfaceTypeDescriptor<IUser> descriptor)
    {
        descriptor.Name("User");
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
    }

    private static IUser? ResolveById(string id)
    {
        var row = TypenameData.FindUser(id);
        if (row is null)
        {
            return null;
        }

        return new Admin { Id = row.Id, IsMain = row.IsMain };
    }
}

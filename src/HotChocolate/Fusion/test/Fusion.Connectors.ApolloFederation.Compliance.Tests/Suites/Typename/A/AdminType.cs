using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Suites.Typename.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Admin</c> entity in the
/// <c>a</c> subgraph
/// (<c>type Admin implements User @key(fields: "id") { id: ID!, isMain: Boolean! }</c>).
/// </summary>
public sealed class AdminType : ObjectType<Admin>
{
    protected override void Configure(IObjectTypeDescriptor<Admin> descriptor)
    {
        descriptor.Implements<UserInterfaceType>();
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.IsMain).Type<NonNullType<BooleanType>>();
    }

    private static Admin? ResolveById(string id)
    {
        var row = TypenameData.FindUser(id);
        return row is null ? null : new Admin { Id = row.Id, IsMain = row.IsMain };
    }
}

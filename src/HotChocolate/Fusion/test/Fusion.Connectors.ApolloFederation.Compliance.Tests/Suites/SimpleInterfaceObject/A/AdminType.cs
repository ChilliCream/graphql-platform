using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Admin</c> entity in the
/// <c>a</c> subgraph
/// (<c>type Admin implements Account @key(fields: "id") { id, isMain, isActive @shareable }</c>).
/// </summary>
public sealed class AdminType : ObjectType<Admin>
{
    protected override void Configure(IObjectTypeDescriptor<Admin> descriptor)
    {
        descriptor.Implements<AccountType>();
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.IsMain).Type<NonNullType<BooleanType>>();
        descriptor.Field(a => a.IsActive).Shareable().Type<NonNullType<BooleanType>>();
    }

    private static Admin? ResolveById(string id)
    {
        var row = AData.FindAccount(id);
        return row is { Typename: "Admin" }
            ? new Admin { Id = row.Id, IsMain = row.IsMain, IsActive = row.IsActive }
            : null;
    }
}

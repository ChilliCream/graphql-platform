using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Accounts;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity owned by the
/// <c>accounts</c> subgraph. Mirrors the audit Schema Definition Language
/// (SDL): <c>type User @key(fields: "id") { id: ID!, name: String,
/// username: String @shareable }</c>.
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
        descriptor.Field(u => u.Name).Type<StringType>();
        descriptor.Field(u => u.Username).Shareable().Type<StringType>();
    }

    private static User? ResolveById(string id)
        => AccountsData.ById.TryGetValue(id, out var user) ? user : null;
}

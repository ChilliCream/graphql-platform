using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.B;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity owned by the
/// <c>b</c> subgraph. Declares <c>@key(fields: "id")</c> with fields
/// <c>id: ID!</c>, <c>name: String</c>, and
/// <c>similarAccounts: [Account!]!</c>.
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

        descriptor
            .Field("similarAccounts")
            .Type<NonNullType<ListType<NonNullType<AccountType>>>>()
            .Resolve(_ => BData.Accounts);
    }

    private static User? ResolveById(string id)
        => BData.UsersById.TryGetValue(id, out var user) ? user : null;
}

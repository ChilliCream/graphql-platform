using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInaccessible.Friends;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity owned by the
/// <c>friends</c> subgraph. Implements:
/// <code>
/// type User @key(fields: "id") {
///   id: ID
///   friends(type: FriendType = FAMILY @inaccessible): [User!]!
///   type: FriendType
/// }
/// </code>
/// The <c>type</c> argument is marked <c>@inaccessible</c>, so the supergraph hides
/// the entire argument while the source schema still applies its <c>FAMILY</c>
/// default. The <c>type</c> field always returns <c>FAMILY</c>; the supergraph nulls
/// that value out because <c>FAMILY</c> is inaccessible.
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<IdType>();

        descriptor
            .Field("friends")
            .Argument(
                "type",
                a => a
                    .Type<FriendTypeType>()
                    .DefaultValue(FriendTypeEnum.FAMILY)
                    .Directive(InaccessibleDirective.Default))
            .Type<NonNullType<ListType<NonNullType<UserType>>>>()
            .Resolve(ctx =>
            {
                var parent = ctx.Parent<User>();
                if (parent.Id is null
                    || !FriendsData.FriendsByUserId.TryGetValue(parent.Id, out var friendIds))
                {
                    return Array.Empty<User>();
                }

                return friendIds
                    .Select(id => FriendsData.ById.TryGetValue(id, out var u) ? u : null)
                    .Where(u => u is not null)
                    .Select(u => u!)
                    .ToArray();
            });

        descriptor
            .Field("type")
            .Type<FriendTypeType>()
            .Resolve(_ => FriendTypeEnum.FAMILY);
    }

    private static User? ResolveById(string id)
        => FriendsData.ById.TryGetValue(id, out var user) ? user : null;
}

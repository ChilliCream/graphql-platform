using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInaccessible.Friends;

/// <summary>
/// Root <c>Query</c> for the <c>friends</c> subgraph. Exposes
/// <c>usersInFriends: [User!]!</c> returning the seeded users with their
/// friend-id lists projected.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("usersInFriends")
            .Type<NonNullType<ListType<NonNullType<UserType>>>>()
            .Resolve(_ => FriendsData.Users);
    }
}

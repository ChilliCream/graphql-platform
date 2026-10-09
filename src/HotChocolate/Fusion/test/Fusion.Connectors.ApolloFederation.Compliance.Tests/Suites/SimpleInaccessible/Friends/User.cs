namespace HotChocolate.Fusion.Suites.SimpleInaccessible.Friends;

/// <summary>
/// The <c>User</c> entity as projected by the <c>friends</c> subgraph. Carries
/// the <c>id</c> only; the friends list is looked up by id from
/// <see cref="FriendsData"/> in the field resolver.
/// </summary>
public sealed class User
{
    public string? Id { get; init; }
}

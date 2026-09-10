namespace HotChocolate.Fusion.Suites.SimpleInaccessible.Friends;

/// <summary>
/// The <c>FriendType</c> enum as projected by the <c>friends</c> subgraph.
/// <c>FAMILY</c> is marked <c>@inaccessible</c> in the supergraph, so the
/// public schema only exposes <c>FRIEND</c>.
/// </summary>
public enum FriendTypeEnum
{
    FAMILY,
    FRIEND
}

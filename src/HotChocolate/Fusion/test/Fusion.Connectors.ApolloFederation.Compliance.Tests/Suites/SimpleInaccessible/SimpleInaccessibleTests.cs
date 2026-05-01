using HotChocolate.Fusion.Suites.SimpleInaccessible.Age;
using HotChocolate.Fusion.Suites.SimpleInaccessible.Friends;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>simple-inaccessible</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two Apollo Federation
/// subgraphs share the <c>User</c> entity. Subgraph <c>age</c> owns
/// <c>id</c> and <c>age</c> and exposes a shareable <c>usersInAge</c> list;
/// subgraph <c>friends</c> owns the <c>friends</c> field, including a
/// <c>type: FriendType = FAMILY @inaccessible</c> argument default and a
/// <c>type: FriendType</c> field whose resolver always returns the
/// inaccessible <c>FAMILY</c> value. The audit verifies that the supergraph
/// hides the <c>@inaccessible</c> argument default and the inaccessible enum
/// value while the source schemas continue to round-trip values internally.
/// </summary>
public sealed class SimpleInaccessibleTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (FriendsSubgraph.Name, FriendsSubgraph.BuildAsync),
            (AgeSubgraph.Name, AgeSubgraph.BuildAsync));

    /// <summary>
    /// <c>usersInAge</c> originates in subgraph <c>age</c>; the planner
    /// enriches each user with <c>friends { id }</c> from subgraph
    /// <c>friends</c>, which uses the <c>@inaccessible</c> default to omit the
    /// <c>type</c> argument from the entity call.
    /// </summary>
    [Fact]
    public Task UsersInAge_Friends_Id() => RunAsync(
        query: """
            query {
              usersInAge {
                id
                friends {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "usersInAge": [
                { "id": "u1", "friends": [{ "id": "u2" }] },
                { "id": "u2", "friends": [{ "id": "u1" }] }
              ]
            }
            """);

    /// <summary>
    /// Same selection as <see cref="UsersInAge_Friends_Id"/> but rooted at
    /// <c>usersInFriends</c>. The query must succeed without specifying the
    /// <c>type</c> argument because its supergraph default has been removed
    /// by <c>@inaccessible</c>.
    /// </summary>
    [Fact]
    public Task UsersInFriends_Friends_Id() => RunAsync(
        query: """
            query {
              usersInFriends {
                id
                friends {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "usersInFriends": [
                { "id": "u1", "friends": [{ "id": "u2" }] },
                { "id": "u2", "friends": [{ "id": "u1" }] }
              ]
            }
            """);

    /// <summary>
    /// Selecting <c>friends(type: FRIEND)</c> succeeds at validation because
    /// <c>FRIEND</c> is the only accessible enum value. The audit expects
    /// errors because the underlying resolver does not project the
    /// <c>type</c> argument and returns <c>FAMILY</c> from <c>type</c>, which
    /// is inaccessible at the supergraph and so must surface as a field
    /// error.
    /// </summary>
    [Fact]
    public Task UsersInFriends_Friends_Type_Friend_Errors() => RunAsync(
        query: """
            query {
              usersInFriends {
                id
                friends(type: FRIEND) {
                  id
                }
              }
            }
            """,
        expectsErrors: true);

    /// <summary>
    /// Selecting <c>type</c> on each friend exposes the inaccessible
    /// <c>FAMILY</c> value returned by the source resolver. The supergraph
    /// must null the value because <c>FAMILY</c> is hidden.
    /// </summary>
    [Fact(Skip = "Gateway forwards inaccessible enum values from the source subgraph instead of nulling them in the response. Same gap as EnumIntersection; see framework-gaps.md 'enum-intersection (partial)'.")]
    public Task UsersInFriends_Friends_Id_Type_Returns_Null_For_Inaccessible_Value() => RunAsync(
        query: """
            query {
              usersInFriends {
                id
                friends {
                  id
                  type
                }
              }
            }
            """,
        expectedData: """
            {
              "usersInFriends": [
                { "id": "u1", "friends": [{ "id": "u2", "type": null }] },
                { "id": "u2", "friends": [{ "id": "u1", "type": null }] }
              ]
            }
            """);
}

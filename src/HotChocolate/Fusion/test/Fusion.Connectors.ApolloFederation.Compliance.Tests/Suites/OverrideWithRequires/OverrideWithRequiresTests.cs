using HotChocolate.Fusion.Suites.OverrideWithRequires.A;
using HotChocolate.Fusion.Suites.OverrideWithRequires.B;
using HotChocolate.Fusion.Suites.OverrideWithRequires.C;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>override-with-requires</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Three Apollo Federation
/// subgraphs share <c>User @key(fields: "id")</c>:
/// <list type="bullet">
/// <item><c>a</c> has <c>name @external</c> and <c>aName @requires(fields: "name")</c>;</item>
/// <item><c>b</c> owns the canonical <c>name</c> via <c>@override(from: "c")</c>;</item>
/// <item><c>c</c> has <c>name @external</c> and <c>cName @requires(fields: "name")</c>.</item>
/// </list>
/// The audit verifies that the planner threads the canonical <c>name</c>
/// (owned by <c>b</c> after the override) into the <c>@requires</c> entity
/// calls in <c>a</c> and <c>c</c>.
/// </summary>
public sealed class OverrideWithRequiresTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync));

    /// <summary>
    /// Three root selections, each pulling <c>id</c>, <c>name</c>,
    /// <c>aName</c>, and <c>cName</c>. Each requires the gateway to fetch
    /// <c>name</c> from <c>b</c> (override owner) and thread it into the
    /// <c>@requires</c> entity calls in <c>a</c> and <c>c</c>.
    /// </summary>
    [Fact]
    public Task UserInA_UserInB_UserInC_All_Fields() => RunAsync(
        query: """
            {
              userInA {
                id
                name
                aName
                cName
              }
              userInB {
                id
                name
                aName
                cName
              }
              userInC {
                id
                name
                aName
                cName
              }
            }
            """,
        expectedData: """
            {
              "userInA": { "id": "u1", "name": "u1-name", "aName": "a__u1-name", "cName": "c__u1-name" },
              "userInB": { "id": "u2", "name": "u2-name", "aName": "a__u2-name", "cName": "c__u2-name" },
              "userInC": { "id": "u3", "name": "u3-name", "aName": "a__u3-name", "cName": "c__u3-name" }
            }
            """);

    /// <summary>
    /// <c>userInC -> cName</c> exercises the <c>@requires(name)</c> path on
    /// subgraph <c>c</c> alone. The planner must fetch <c>name</c> from
    /// <c>b</c> (override owner) and pass it to <c>c</c>'s entity lookup.
    /// </summary>
    [Fact]
    public Task UserInC_CName_Requires_Name_From_Override_Owner() => RunAsync(
        query: """
            query {
              userInC {
                cName
              }
            }
            """,
        expectedData: """
            {
              "userInC": { "cName": "c__u3-name" }
            }
            """);

    /// <summary>
    /// <c>userInA -> cName</c> hops <c>a</c> for the user, fetches the
    /// canonical <c>name</c> from <c>b</c>, and threads it into <c>c</c>'s
    /// entity lookup to produce <c>cName</c>.
    /// </summary>
    [Fact]
    public Task UserInA_CName_Routes_Across_Three_Subgraphs() => RunAsync(
        query: """
            query {
              userInA {
                cName
              }
            }
            """,
        expectedData: """
            {
              "userInA": { "cName": "c__u1-name" }
            }
            """);

    /// <summary>
    /// <c>userInA -> aName</c> hops <c>a</c> for the user, fetches
    /// <c>name</c> from <c>b</c>, and threads it back into <c>a</c>'s
    /// entity lookup to compute <c>aName</c>.
    /// </summary>
    [Fact]
    public Task UserInA_AName_Routes_Through_Override_Owner() => RunAsync(
        query: """
            query {
              userInA {
                aName
              }
            }
            """,
        expectedData: """
            {
              "userInA": { "aName": "a__u1-name" }
            }
            """);
}

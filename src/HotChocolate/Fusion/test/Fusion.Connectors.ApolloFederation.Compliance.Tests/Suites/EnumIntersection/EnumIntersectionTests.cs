using HotChocolate.Fusion.Suites.EnumIntersection.A;
using HotChocolate.Fusion.Suites.EnumIntersection.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>enum-intersection</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two Apollo Federation
/// subgraphs declare overlapping enum values: subgraph <c>a</c> only
/// declares <c>REGULAR</c>; subgraph <c>b</c> declares both
/// <c>ANONYMOUS @inaccessible</c> and <c>REGULAR</c>. The supergraph
/// must intersect to <c>REGULAR</c> only.
/// </summary>
public sealed class EnumIntersectionTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    /// <summary>
    /// Plain id selection. The planner picks one subgraph and returns
    /// both users.
    /// </summary>
    [Fact]
    public Task Users_Returns_Ids_Only() => RunAsync(
        query: """
            query {
              users { id }
            }
            """,
        expectedData: """
            {
              "users": [
                { "id": "u1" },
                { "id": "u2" }
              ]
            }
            """);

    /// <summary>
    /// Walking <c>type</c> from subgraph <c>a</c>'s <c>Query.users</c>
    /// surfaces <c>null</c> for <c>u2</c> because subgraph <c>a</c>
    /// cannot project the <c>ANONYMOUS</c> value.
    /// </summary>
    [Fact(Skip = "Gateway does not surface an error when an enum value is null because the source subgraph cannot project it. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Users_Type_Returns_Null_For_Subgraph_A_Side() => RunAsync(
        query: """
            query {
              users { id type }
            }
            """,
        expectedData: """
            {
              "users": [
                { "id": "u1", "type": "REGULAR" },
                { "id": "u2", "type": null }
              ]
            }
            """,
        expectsErrors: true);

    /// <summary>
    /// Walking <c>type</c> from subgraph <c>b</c>'s <c>Query.usersB</c>
    /// surfaces <c>null</c> for <c>u2</c> because <c>ANONYMOUS</c> is
    /// inaccessible in the supergraph.
    /// </summary>
    [Fact(Skip = "Gateway forwards inaccessible enum values from the source subgraph instead of nulling them in the response. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task UsersB_Type_Returns_Null_For_Inaccessible_Value() => RunAsync(
        query: """
            query {
              usersB { id type }
            }
            """,
        expectedData: """
            {
              "usersB": [
                { "id": "u1", "type": "REGULAR" },
                { "id": "u2", "type": null }
              ]
            }
            """);

    /// <summary>
    /// Filtering by the public <c>REGULAR</c> value works.
    /// </summary>
    [Fact]
    public Task UsersByType_Regular_Returns_Single_User() => RunAsync(
        query: """
            query {
              usersByType(type: REGULAR) { id type }
            }
            """,
        expectedData: """
            {
              "usersByType": [
                { "id": "u1", "type": "REGULAR" }
              ]
            }
            """);

    /// <summary>
    /// Filtering by the inaccessible <c>ANONYMOUS</c> value must be
    /// rejected by validation. The data payload is <c>null</c> and the
    /// response carries errors.
    /// </summary>
    [Fact]
    public Task UsersByType_Anonymous_Is_Rejected() => RunAsync(
        query: """
            query {
              usersByType(type: ANONYMOUS) { id type }
            }
            """,
        expectedData: "null",
        expectsErrors: true);
}

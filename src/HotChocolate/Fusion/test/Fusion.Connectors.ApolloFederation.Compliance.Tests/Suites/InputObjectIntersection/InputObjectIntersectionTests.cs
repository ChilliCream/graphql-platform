using HotChocolate.Fusion.Suites.InputObjectIntersection.A;
using HotChocolate.Fusion.Suites.InputObjectIntersection.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>input-object-intersection</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two Apollo Federation
/// subgraphs each declare a <c>UsersFilter</c> input object with a
/// different field set; the supergraph must expose only the
/// intersection (<c>first</c> only).
/// </summary>
public sealed class InputObjectIntersectionTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    /// <summary>
    /// The <c>first</c> field is in the intersection so the query
    /// succeeds.
    /// </summary>
    [Fact]
    public Task UsersInA_With_First_Returns_All_Users() => RunAsync(
        query: """
            query {
              usersInA(filter: { first: 1 }) { id }
            }
            """,
        expectedData: """
            {
              "usersInA": [
                { "id": "u1" },
                { "id": "u2" }
              ]
            }
            """);

    /// <summary>
    /// <c>offset</c> is only declared by subgraph <c>b</c> and therefore
    /// not part of the supergraph's intersection. Validation must
    /// reject the query.
    /// </summary>
    [Fact]
    public Task UsersInA_With_Offset_Is_Rejected() => RunAsync(
        query: """
            query {
              usersInA(filter: { first: 1, offset: 2 }) { id }
            }
            """,
        expectsErrors: true);

    /// <summary>
    /// Even on subgraph <c>b</c>'s own root field, the supergraph
    /// intersection forbids <c>offset</c>.
    /// </summary>
    [Fact]
    public Task UsersInB_With_Offset_Is_Rejected() => RunAsync(
        query: """
            query {
              usersInB(filter: { first: 1, offset: 2 }) { id }
            }
            """,
        expectsErrors: true);
}

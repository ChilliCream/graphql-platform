using HotChocolate.Fusion.Suites.UnavailableOverride.A;
using HotChocolate.Fusion.Suites.UnavailableOverride.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>unavailable-override</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two Apollo Federation
/// subgraphs share <c>Post @key(fields: "id")</c>. Subgraph <c>b</c>
/// declares <c>createdAt @override(from: "non-existing")</c>; because the
/// named source schema does not participate in the supergraph the override
/// has no effect, and both subgraphs serve identical canonical data. The
/// audit verifies that the gateway does not reject the composition and
/// happily routes <c>createdAt</c> through either subgraph.
/// </summary>
public sealed class UnavailableOverrideTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    /// <summary>
    /// <c>feed { createdAt }</c> through the shareable list root. The
    /// gateway picks one subgraph for the list; both subgraphs hold the
    /// canonical <c>createdAt</c> values so the response is stable.
    /// </summary>
    [Fact]
    public Task Feed_CreatedAt_Returns_Canonical_Values() => RunAsync(
        query: """
            query {
              feed {
                createdAt
              }
            }
            """,
        expectedData: """
            {
              "feed": [
                { "createdAt": "p1-createdAt" },
                { "createdAt": "p2-createdAt" }
              ]
            }
            """);

    /// <summary>
    /// Mixed roots: <c>aFeed</c> from subgraph <c>a</c> yields the second
    /// post; <c>bFeed</c> from subgraph <c>b</c> yields the first post.
    /// Both serve <c>createdAt</c> directly without an override hop.
    /// </summary>
    [Fact]
    public Task AFeed_And_BFeed_CreatedAt_From_Local_Subgraphs() => RunAsync(
        query: """
            query {
              aFeed {
                createdAt
              }
              bFeed {
                createdAt
              }
            }
            """,
        expectedData: """
            {
              "aFeed": [
                { "createdAt": "p2-createdAt" }
              ],
              "bFeed": [
                { "createdAt": "p1-createdAt" }
              ]
            }
            """);
}

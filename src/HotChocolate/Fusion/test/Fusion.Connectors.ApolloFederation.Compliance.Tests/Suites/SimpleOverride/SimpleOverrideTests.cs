using HotChocolate.Fusion.Suites.SimpleOverride.A;
using HotChocolate.Fusion.Suites.SimpleOverride.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>simple-override</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two Apollo Federation
/// subgraphs share <c>Post @key(fields: "id")</c>. Subgraph <c>a</c>
/// declares a hardcoded <c>createdAt</c> resolver returning <c>"NEVER"</c>;
/// subgraph <c>b</c> overrides the field via <c>@override(from: "a")</c>
/// and returns the canonical seeded values. Both subgraphs expose a
/// shareable <c>feed</c> plus a private <c>aFeed</c>/<c>bFeed</c>.
/// </summary>
public sealed class SimpleOverrideTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    /// <summary>
    /// <c>feed { createdAt }</c> originates as a shareable list. The gateway
    /// must route <c>createdAt</c> to subgraph <c>b</c> (the override owner)
    /// regardless of which subgraph supplies the parent <c>feed</c> list.
    /// </summary>
    [Fact]
    public Task Feed_CreatedAt_Routes_To_Override_Owner() => RunAsync(
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
    /// Mixed roots: <c>aFeed</c> originates in subgraph <c>a</c> and yields
    /// the second post; <c>bFeed</c> originates in subgraph <c>b</c> and
    /// yields the first post. Both must resolve <c>createdAt</c> through the
    /// override owner so the value matches the canonical seed.
    /// </summary>
    [Fact]
    public Task AFeed_And_BFeed_CreatedAt_Through_Override_Owner() => RunAsync(
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

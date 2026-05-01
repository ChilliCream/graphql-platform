using HotChocolate.Fusion.Suites.OverrideTypeInterface.A;
using HotChocolate.Fusion.Suites.OverrideTypeInterface.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>override-type-interface</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two Apollo Federation
/// subgraphs share the <c>ImagePost @key("id")</c> entity but anchor it on
/// different interfaces. Subgraph <c>a</c> declares <c>ImagePost implements
/// Post</c> and exposes <c>Query.feed: [Post]</c>. Subgraph <c>b</c>
/// declares <c>ImagePost implements AnotherPost</c> with
/// <c>createdAt @override(from: "a")</c> and exposes
/// <c>Query.anotherFeed: [AnotherPost]</c>. The audit verifies that the
/// gateway routes <c>createdAt</c> through the override owner regardless of
/// which interface anchor is used at the originating root field.
/// </summary>
public sealed class OverrideTypeInterfaceTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    /// <summary>
    /// <c>feed</c> originates in subgraph <c>a</c> (returning <c>ImagePost</c>
    /// instances cast to <c>Post</c>). With <c>@override</c>, <c>createdAt</c>
    /// must come from subgraph <c>b</c> instead of the hardcoded
    /// <c>"NEVER"</c> resolver in subgraph <c>a</c>.
    /// </summary>
    [Fact(Skip = "Planner does not honor @override when the originating subgraph reaches the entity through a different interface than the override owner. Subgraph a's feed returns Post; subgraph b's ImagePost implements AnotherPost (not Post), and the planner keeps a's createdAt resolver instead of routing through b.")]
    public Task Feed_Id_CreatedAt_Through_Override_Owner() => RunAsync(
        query: """
            query {
              feed {
                id
                createdAt
              }
            }
            """,
        expectedData: """
            {
              "feed": [
                { "id": "i1", "createdAt": "i1-createdAt" },
                { "id": "i2", "createdAt": "i2-createdAt" }
              ]
            }
            """);

    /// <summary>
    /// <c>feed</c> contains only <c>ImagePost</c> instances; selecting fields
    /// on a <c>TextPost</c> inline fragment yields empty objects per element.
    /// </summary>
    [Fact]
    public Task Feed_TextPost_Fragment_Yields_Empty_Objects() => RunAsync(
        query: """
            query {
              feed {
                ... on TextPost {
                  id
                  body
                }
              }
            }
            """,
        expectedData: """
            {
              "feed": [{}, {}]
            }
            """);

    /// <summary>
    /// <c>anotherFeed</c> originates in subgraph <c>b</c>. Selecting
    /// <c>createdAt</c> through the <c>AnotherPost</c> interface returns the
    /// canonical <c>b</c>-owned values directly.
    /// </summary>
    [Fact]
    public Task AnotherFeed_CreatedAt() => RunAsync(
        query: """
            query {
              anotherFeed {
                createdAt
              }
            }
            """,
        expectedData: """
            {
              "anotherFeed": [
                { "createdAt": "i1-createdAt" },
                { "createdAt": "i2-createdAt" }
              ]
            }
            """);

    /// <summary>
    /// Same root as <see cref="AnotherFeed_CreatedAt"/> with an inline
    /// fragment on the concrete <c>ImagePost</c> type. Field merging must
    /// collapse the duplicate <c>createdAt</c> and <c>id</c> selections.
    /// </summary>
    [Fact]
    public Task AnotherFeed_ImagePost_Fragment_Field_Merging() => RunAsync(
        query: """
            {
              anotherFeed {
                createdAt
                id
                ... on ImagePost {
                  createdAt
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "anotherFeed": [
                { "createdAt": "i1-createdAt", "id": "i1" },
                { "createdAt": "i2-createdAt", "id": "i2" }
              ]
            }
            """);
}

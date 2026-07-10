using HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphA;
using HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphB;
using HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphC;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>provides-on-union</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Three subgraphs
/// (<c>a</c>, <c>b</c>, <c>c</c>) verify that <c>@provides</c> works
/// correctly with union types and inline fragments.
/// </summary>
public sealed class ProvidesOnUnionTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (SubgraphASubgraph.Name, SubgraphASubgraph.BuildAsync),
            (SubgraphBSubgraph.Name, SubgraphBSubgraph.BuildAsync),
            (SubgraphCSubgraph.Name, SubgraphCSubgraph.BuildAsync));

    /// <summary>
    /// Selects <c>title</c> only on <c>Book</c> fragments. Subgraph <c>b</c>
    /// provides <c>Book.title</c> inline through <c>@provides</c>, so no
    /// entity call to subgraph <c>c</c> is needed for the book title.
    /// </summary>
    [Fact]
    public Task Media_Book_Title_Without_Movie_Title() => RunAsync(
        query: """
            query {
              media {
                ... on Book {
                  id
                  title
                }
                ... on Movie {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "media": [
                { "id": "m1", "title": "Book 1" },
                { "id": "m2" }
              ]
            }
            """);

    /// <summary>
    /// Selects <c>title</c> on both <c>Book</c> and <c>Movie</c>. Book title
    /// comes from subgraph <c>b</c> via <c>@provides</c>, Movie title comes
    /// from subgraph <c>c</c> via entity resolution.
    /// </summary>
    [Fact]
    public Task Media_Book_And_Movie_Titles() => RunAsync(
        query: """
            query {
              media {
                ... on Book {
                  id
                  title
                }
                ... on Movie {
                  id
                  title
                }
              }
            }
            """,
        expectedData: """
            {
              "media": [
                { "id": "m1", "title": "Book 1" },
                { "id": "m2", "title": "Movie 1" }
              ]
            }
            """);
}

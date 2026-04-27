using HotChocolate.Fusion.Suites.NullKeys.A;
using HotChocolate.Fusion.Suites.NullKeys.B;
using HotChocolate.Fusion.Suites.NullKeys.C;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>null-keys</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Three Apollo Federation
/// subgraphs share the <c>Book</c> entity. Subgraph <c>a</c> exposes
/// <c>bookContainers</c> and only the <c>upc</c> key; <c>b</c> bridges
/// between <c>upc</c> and <c>id</c>; <c>c</c> owns <c>author</c> via the
/// <c>id</c> key. The third book triggers <c>b</c>'s reference resolver
/// to return <c>null</c>, which must propagate as <c>author: null</c>
/// without aborting the parent list.
/// </summary>
public sealed class NullKeysTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync));

    /// <summary>
    /// Walks the three-subgraph chain and verifies that the null entity
    /// returned for the third book leaves the parent list intact, with the
    /// downstream <c>author</c> field set to <c>null</c>.
    /// </summary>
    [Fact]
    public Task BookContainers_Resolves_Null_Author_When_Bridge_Subgraph_Returns_Null() => RunAsync(
        query: """
            query {
              bookContainers {
                book {
                  upc
                  author {
                    name
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "bookContainers": [
                { "book": { "upc": "b1", "author": { "name": "Alice" } } },
                { "book": { "upc": "b2", "author": { "name": "Bob" } } },
                { "book": { "upc": "b3", "author": null } }
              ]
            }
            """);
}

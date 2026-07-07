using HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;
using HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.B;
using HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.C;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>interface-object-indirect-extension</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. The gateway composes three
/// Apollo Federation subgraphs: <c>a</c> owns the <c>Media</c> interface and its
/// <c>Video</c> / <c>Article</c> implementations, <c>b</c> owns <c>Author</c> and
/// indirectly extends <c>Video</c> with <c>authorName</c>, and <c>c</c> owns
/// <c>Playlist</c> plus a <c>Media @interfaceObject</c> stand-in.
/// </summary>
public sealed class InterfaceObjectIndirectExtensionTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync));

    [Fact]
    public Task Media_Author_And_Playlist_ResolveThroughInterfaceObject() => RunAsync(
        query: """
            query {
              media {
                id
                title
                ... on Video {
                  duration
                  authorName
                }
                ... on Article {
                  wordCount
                }
              }
              author {
                id
                name
              }
              playlist {
                id
                name
                media {
                  id
                  ... on Video {
                    duration
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "media": {
                "id": "1",
                "title": "title for 1",
                "duration": 100,
                "authorName": "John Doe"
              },
              "author": {
                "id": "1",
                "name": "name for 1"
              },
              "playlist": {
                "id": "1",
                "name": "name for 1",
                "media": {
                  "id": "1",
                  "duration": 100
                }
              }
            }
            """);
}

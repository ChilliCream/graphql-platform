using HotChocolate.Fusion.Suites.UnionIntersection.A;
using HotChocolate.Fusion.Suites.UnionIntersection.B;

namespace HotChocolate.Fusion.Suites;

public sealed class UnionIntersectionTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Fact]
    public Task Media_Should_ReturnEmpty_When_MovieFragment() => RunAsync(
        query: """
            query {
              media {
                ... on Movie {
                  title
                }
              }
            }
            """,
        expectedData: """
            {
              "media": {}
            }
            """);

    [Fact]
    public Task Media_Should_ReturnTitle_When_BookFragment() => RunAsync(
        query: """
            query {
              media {
                ... on Book {
                  title
                }
              }
            }
            """,
        expectedData: """
            {
              "media": {
                "title": "The Lord of the Rings"
              }
            }
            """);

    [Fact]
    public Task Media_Should_ReturnTitle_When_BookAndMovieFragments() => RunAsync(
        query: """
            query {
              media {
                ... on Book {
                  title
                }
                ... on Movie {
                  title
                }
              }
            }
            """,
        expectedData: """
            {
              "media": {
                "title": "The Lord of the Rings"
              }
            }
            """);

    [Fact]
    public Task Viewer_Should_ReturnAllMedia_When_AllFragments() => RunAsync(
        query: """
            query {
              viewer {
                media {
                  __typename
                  ... on Song {
                    title
                  }
                  ... on Movie {
                    title
                  }
                  ... on Book {
                    title
                  }
                }
                book {
                  __typename
                  ... on Song {
                    title
                  }
                  ... on Movie {
                    title
                  }
                  ... on Book {
                    title
                  }
                }
                song {
                  __typename
                  ... on Song {
                    title
                  }
                  ... on Movie {
                    title
                  }
                  ... on Book {
                    title
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "viewer": {
                "media": {
                  "__typename": "Book",
                  "title": "The Lord of the Rings"
                },
                "book": {
                  "__typename": "Book",
                  "title": "The Lord of the Rings"
                },
                "song": {
                  "__typename": "Song",
                  "title": "Song Title"
                }
              }
            }
            """);

    [Fact]
    public Task ViewerMedia_Should_ReturnEmpty_When_MovieFragment() => RunAsync(
        query: """
            query {
              viewer {
                media {
                  ... on Movie {
                    title
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "viewer": {
                "media": {}
              }
            }
            """);

    [Fact]
    public Task ViewerMedia_Should_ReturnTitle_When_BookFragment() => RunAsync(
        query: """
            query {
              viewer {
                media {
                  ... on Book {
                    title
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "viewer": {
                "media": {
                  "title": "The Lord of the Rings"
                }
              }
            }
            """);

    [Fact]
    public Task ViewerMedia_Should_ReturnTitle_When_BookAndMovieFragments() => RunAsync(
        query: """
            query {
              viewer {
                media {
                  ... on Book {
                    title
                  }
                  ... on Movie {
                    title
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "viewer": {
                "media": {
                  "title": "The Lord of the Rings"
                }
              }
            }
            """);

    [Fact]
    public Task Viewer_Should_ReturnAllMedia_When_AllFragmentsRepeated() => RunAsync(
        query: """
            query {
              viewer {
                media {
                  __typename
                  ... on Song {
                    title
                  }
                  ... on Movie {
                    title
                  }
                  ... on Book {
                    title
                  }
                }
                book {
                  __typename
                  ... on Song {
                    title
                  }
                  ... on Movie {
                    title
                  }
                  ... on Book {
                    title
                  }
                }
                song {
                  __typename
                  ... on Song {
                    title
                  }
                  ... on Movie {
                    title
                  }
                  ... on Book {
                    title
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "viewer": {
                "media": {
                  "__typename": "Book",
                  "title": "The Lord of the Rings"
                },
                "book": {
                  "__typename": "Book",
                  "title": "The Lord of the Rings"
                },
                "song": {
                  "__typename": "Song",
                  "title": "Song Title"
                }
              }
            }
            """);

    [Fact]
    public Task AMedia_Should_ReturnEmpty_When_MovieFragment() => RunAsync(
        query: """
            query {
              aMedia {
                ... on Movie {
                  title
                  bTitle
                }
              }
            }
            """,
        expectedData: """
            {
              "aMedia": {}
            }
            """);

    [Fact]
    public Task AMedia_Should_ReturnAllTitles_When_BookFragment() => RunAsync(
        query: """
            query {
              aMedia {
                ... on Book {
                  title
                  aTitle
                  bTitle
                }
              }
            }
            """,
        expectedData: """
            {
              "aMedia": {
                "title": "The Lord of the Rings",
                "aTitle": "A: The Lord of the Rings",
                "bTitle": "B: The Lord of the Rings"
              }
            }
            """);

    [Fact]
    public Task Viewer_Should_ReturnCrossSubgraphTitles_When_AllFragments() => RunAsync(
        query: """
            query {
              viewer {
                media {
                  __typename
                  ... on Song {
                    title
                    aTitle
                  }
                  ... on Movie {
                    title
                    bTitle
                  }
                  ... on Book {
                    title
                    aTitle
                    bTitle
                  }
                }
                book {
                  __typename
                  ... on Song {
                    title
                    aTitle
                  }
                  ... on Movie {
                    title
                    bTitle
                  }
                  ... on Book {
                    title
                    aTitle
                    bTitle
                  }
                }
                song {
                  __typename
                  ... on Song {
                    title
                    aTitle
                  }
                  ... on Movie {
                    title
                    bTitle
                  }
                  ... on Book {
                    title
                    aTitle
                    bTitle
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "viewer": {
                "media": {
                  "__typename": "Book",
                  "title": "The Lord of the Rings",
                  "aTitle": "A: The Lord of the Rings",
                  "bTitle": "B: The Lord of the Rings"
                },
                "book": {
                  "__typename": "Book",
                  "title": "The Lord of the Rings",
                  "aTitle": "A: The Lord of the Rings",
                  "bTitle": "B: The Lord of the Rings"
                },
                "song": {
                  "__typename": "Song",
                  "title": "Song Title",
                  "aTitle": "A: Song Title"
                }
              }
            }
            """);

    [Fact]
    public Task Viewer_Should_ReturnBMediaMovie_When_AMediaAndBMediaMovieFragments() => RunAsync(
        query: """
            query {
              viewer {
                aMedia {
                  ... on Movie {
                    title
                    bTitle
                  }
                }
                bMedia {
                  ... on Movie {
                    title
                    bTitle
                  }
                }
              }
            }
            """,
        expectedData: """
            {
              "viewer": {
                "aMedia": {},
                "bMedia": {
                  "title": "A Movie Title",
                  "bTitle": "B Movie Title"
                }
              }
            }
            """);
}

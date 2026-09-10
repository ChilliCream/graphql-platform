using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

// Mirrors the interface-object-indirect-extension audit shape natively: an @interfaceObject
// stand-in value reached through a relationship (playlist.media) rather than a root list.
// Source schema A owns the Media interface, its Video/Article implementations, and the covering
// interface lookup. Source schema C owns Playlist and an @interfaceObject Media stand-in; the
// media relationship yields an opaque value whose concrete identity is recovered through A.
public class InterfaceObjectNestedTests : FusionTestBase
{
    private const string SchemaA =
        """
        type Query {
          media: Media
          mediaById(id: ID!): Media @lookup
        }

        interface Media {
          id: ID!
          title: String!
        }

        type Video implements Media @key(fields: "id") {
          id: ID!
          title: String!
          duration: Int!
        }

        type Article implements Media @key(fields: "id") {
          id: ID!
          title: String!
          wordCount: Int!
        }
        """;

    private const string SchemaC =
        """
        type Query {
          playlist: Playlist
        }

        type Playlist @key(fields: "id") {
          id: ID!
          name: String!
          media: Media
        }

        type Media @interfaceObject @key(fields: "id") {
          id: ID!
        }
        """;

    [Fact]
    public async Task Playlist_Media_ResolvesConcreteFieldThroughInterfaceObject()
    {
        // arrange
        using var serverA = CreateSourceSchema("A", SchemaA);
        using var serverC = CreateSourceSchema("C", SchemaC);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("C", serverC)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              playlist {
                id
                name
                media {
                  id
                  ... on Video { duration }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }
}

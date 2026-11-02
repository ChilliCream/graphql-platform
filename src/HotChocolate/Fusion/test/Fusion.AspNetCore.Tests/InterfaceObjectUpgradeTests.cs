using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class InterfaceObjectUpgradeTests : FusionTestBase
{
    private const string SchemaA =
        """
        type Query {
          mediaById(id: ID!): Media @lookup
        }

        interface Media {
          id: ID!
          credits: Credits!
          tags: [String!]!
        }

        type Credits {
          director: String!
        }

        type Book implements Media @key(fields: "id") {
          id: ID!
          credits: Credits!
          tags: [String!]!
        }

        type Movie implements Media @key(fields: "id") {
          id: ID!
          credits: Credits!
          tags: [String!]!
        }
        """;

    private const string SchemaB =
        """
        type Query {
          trendingMedia: [Media!]!
          mediaByKey(id: ID!): Media @lookup @internal
        }

        type Media @interfaceObject @key(fields: "id") {
          id: ID!
          views: Int!
        }
        """;

    [Fact]
    public async Task Upgrade_Should_CompleteObjectField_When_StandInLeftItUndefined()
    {
        // arrange
        using var serverA = CreateSourceSchema("A", SchemaA);
        using var serverB = CreateSourceSchema("B", SchemaB);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              trendingMedia {
                __typename
                views
                credits {
                  director
                }
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Upgrade_Should_CompleteListField_When_StandInLeftItUndefined()
    {
        // arrange
        using var serverA = CreateSourceSchema("A", SchemaA);
        using var serverB = CreateSourceSchema("B", SchemaB);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              trendingMedia {
                __typename
                views
                tags
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }
}

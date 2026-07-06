using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class OrganizationsConnectionReproTests : FusionTestBase
{
    // Repro for customer report: a Relay connection query with `first: 100` against a
    // Fusion gateway produces a corrupted subgraph request, causing the subgraph to fail
    // parsing with "Invalid number, expected digit but got: `c`" (HC0011).
    // MatchSnapshotAsync re-parses every captured subgraph request body, so a corrupted
    // `first: 100` literal surfaces as a SyntaxException there.
    [Fact]
    public async Task Organizations_Connection_With_Fragment_And_Typename()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              organizations(first: Int, after: String): OrganizationConnection
            }

            type OrganizationConnection {
              pageInfo: PageInfo!
              edges: [OrganizationEdge!]
            }

            type OrganizationEdge {
              cursor: String!
              node: Organization!
            }

            type PageInfo {
              hasNextPage: Boolean!
              endCursor: String
            }

            type Organization {
              id: ID!
              Number: Int!
              displayName: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query Organizations($cursor: String) {
              organizations(first: 100, after: $cursor) {
                pageInfo {
                  hasNextPage
                  endCursor
                  __typename
                }
                edges {
                  node {
                    ...Organization
                    __typename
                  }
                  __typename
                }
                __typename
              }
            }
            fragment Organization on Organization {
              id
              Number
              displayName
              __typename
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

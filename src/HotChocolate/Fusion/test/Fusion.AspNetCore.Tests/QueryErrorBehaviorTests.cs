using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using OperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.Fusion;

/// <summary>
/// Error-behavior coverage for queries across the failure categories Fusion has to integrate:
/// a transport error on the root subgraph (data null + single unexpected error) and a coercion
/// error raised when a wrong-typed entity key flows into a relay node enrichment lookup.
/// </summary>
public class QueryErrorBehaviorTests : FusionTestBase
{
    [Fact]
    public async Task Query_Should_ReturnDataNullAndSingleUnexpectedError_When_RootSubgraphUnavailable()
    {
        // arrange
        // A is the only subgraph and is offline, so every gateway -> subgraph call returns 500.
        using var a = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", a)],
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              viewer {
                name
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": null,
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "path": [
                    "viewer"
                  ]
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Query_Should_IntegrateCoercionError_When_LookupKeyHasInvalidFormat()
    {
        // arrange
        // The source returns a string ID that the Relay lookup cannot decode.
        using var a = CreateSourceSchema(
            "A",
            """
            type Query {
              review: Review
            }

            type Review {
              id: ID! @shareable
            }
            """,
            mockHttpResponse: _ => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"data":{"review":{"id":"1","__typename":"Review"}}}""",
                        Encoding.UTF8,
                        new MediaTypeHeaderValue("application/json"))
                }));

        using var b = CreateSourceSchema(
            "B",
            builder => builder
                .AddQueryType<ReviewsApi.Query>()
                .AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .ModifyOptions(o => o.StrictValidation = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", a), ("B", b)],
            configureGatewayBuilder: builder =>
                builder.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              review {
                id
                body
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "review": null
              },
              "errors": [
                {
                  "message": "The node ID string has an invalid format.",
                  "path": [
                    "review",
                    "body"
                  ],
                  "extensions": {
                    "originalValue": "1"
                  }
                }
              ]
            }
            """);
    }

    public static class ReviewsApi
    {
        public class Query
        {
            // Mirrors the relay Node lookup of the demo's Reviews subgraph.
            [Lookup, NodeResolver]
            public Review? GetReviewById(int id)
                => new Review(id, "A great read");
        }

        public record Review(int Id, string Body);
    }
}

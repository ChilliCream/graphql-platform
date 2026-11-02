using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HotChocolate.Resolvers;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using OperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.Fusion;

/// <summary>
/// Error-behavior coverage for mutations whose payload is enriched by a second subgraph through an
/// entity lookup. Exercises a transport-unavailable lookup, a subgraph field error, a wrong-typed
/// relay node key, and an unavailable non-null mutation root.
/// </summary>
public class MutationErrorBehaviorTests : FusionTestBase
{
    [Fact]
    public async Task Mutation_Should_NullFieldAndReportError_When_LookupSubgraphUnavailable()
    {
        // arrange
        // REVIEWS owns the mutation and body; STARS provides the non-null stars via a lookup.
        using var reviews = CreateSourceSchema(
            "REVIEWS",
            """
            type Mutation {
              createReview(body: String!): Review
            }

            type Query {
              version: String
              reviewById(id: ID!): Review @lookup @internal
            }

            type Review {
              id: ID!
              body: String!
            }
            """);

        using var stars = CreateSourceSchema(
            "STARS",
            """
            type Query {
              reviewById(id: ID!): Review @lookup @internal
            }

            type Review {
              id: ID!
              stars: Int!
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
            [("REVIEWS", reviews), ("STARS", stars)],
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            mutation {
              createReview(body: "great") {
                id
                body
                stars
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
                "createReview": null
              },
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "path": [
                    "createReview",
                    "stars"
                  ]
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Mutation_Should_IntegrateSubgraphError_When_LookupSubgraphErrors()
    {
        // arrange
        // Both subgraphs are typed so STARS can throw a distinctive resolver error (custom message
        // and code) for stars. This proves the real subgraph error is integrated verbatim at the
        // gateway path, as opposed to being masked like a raw transport failure.
        using var reviews = CreateSourceSchema(
            "REVIEWS",
            builder => builder
                .AddMutationType<ReviewsMutationApi.Mutation>()
                .AddQueryType<ReviewsMutationApi.Query>());

        using var stars = CreateSourceSchema(
            "STARS",
            builder => builder.AddQueryType<StarsApi.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("REVIEWS", reviews), ("STARS", stars)],
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            mutation {
              createReview(body: "great") {
                id
                body
                stars
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
                "createReview": null
              },
              "errors": [
                {
                  "message": "Stars are temporarily unavailable.",
                  "path": [
                    "createReview",
                    "stars"
                  ],
                  "extensions": {
                    "code": "STARS_UNAVAILABLE"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Mutation_Should_IntegrateCoercionError_When_LookupKeyHasWrongType()
    {
        // arrange
        // REVIEWS owns the mutation and returns a Review whose id key is a raw JSON int instead of
        // an encoded relay global id. The hand-crafted response bypasses normal id serialization.
        using var reviews = CreateSourceSchema(
            "REVIEWS",
            """
            type Mutation {
              createReview(body: String!): Review
            }

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
                        """{"data":{"createReview":{"id":1,"__typename":"Review"}}}""",
                        Encoding.UTF8,
                        new MediaTypeHeaderValue("application/json"))
                }));

        // STARS is the relay Node owner, so enriching Review.body runs through node(id: ID!). The
        // raw int id is fed where an encoded global id (string) is required, so node rejects it.
        using var stars = CreateSourceSchema(
            "STARS",
            builder => builder
                .AddQueryType<ReviewsApi.Query>()
                .AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .ModifyOptions(o => o.StrictValidation = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("REVIEWS", reviews), ("STARS", stars)],
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            mutation {
              createReview(body: "great") {
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
                "createReview": null
              },
              "errors": [
                {
                  "message": "The node ID string has an invalid format.",
                  "path": [
                    "createReview",
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

    [Fact]
    public async Task Mutation_Should_ReturnDataNullAndSingleUnexpectedError_When_RootMutationSubgraphUnavailable()
    {
        // arrange
        // The mutation root is non-null and REVIEWS is offline, so every gateway -> subgraph call
        // returns 500 and the non-null root null-propagates to the operation root.
        using var reviews = CreateSourceSchema(
            "REVIEWS",
            """
            type Mutation {
              createReview(body: String!): Review!
            }

            type Query {
              version: String
              reviewById(id: ID!): Review @lookup @internal
            }

            type Review {
              id: ID!
              body: String!
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
            [("REVIEWS", reviews)],
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            mutation {
              createReview(body: "x") {
                id
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
                    "createReview"
                  ]
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

    // REVIEWS subgraph for the subgraph-error case: owns the mutation and body.
    public static class ReviewsMutationApi
    {
        public class Mutation
        {
            public Review? CreateReview(string body) => new(1, body);
        }

        public class Query
        {
            public string Version => "1.0.0";
        }

        [EntityKey("id")]
        public record Review(int Id, string Body);
    }

    // STARS subgraph for the subgraph-error case: provides stars via a lookup, and stars throws a
    // distinctive business error so we can prove the real subgraph message and code are integrated.
    public static class StarsApi
    {
        public class Query
        {
            [Internal, Lookup]
            public Review? GetReviewById(int id) => new(id);
        }

        public record Review(int Id)
        {
            public int GetStars(IResolverContext context)
                => throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("Stars are temporarily unavailable.")
                        .SetCode("STARS_UNAVAILABLE")
                        .SetPath(context.Path)
                        .Build());
        }
    }
}

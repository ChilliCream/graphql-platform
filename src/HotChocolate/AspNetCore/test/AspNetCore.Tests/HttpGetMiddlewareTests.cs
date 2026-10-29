using System.Net;
using System.Text;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public class HttpGetMiddlewareTests : ServerTestBase
{
    public HttpGetMiddlewareTests(TestServerFactory serverFactory)
        : base(serverFactory) { }

    [Fact]
    public async Task Simple_IsAlive_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await server.GetAsync(
            new ClientQueryRequest { Query = "{ __typename }" });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MapGraphQLHttp_Simple_IsAlive_Test()
    {
        // arrange
        var server = CreateServer(endpoint => endpoint.MapGraphQLHttp());

        // act
        var result = await server.GetAsync(
            new ClientQueryRequest { Query = "{ __typename }" });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MapGraphQLHttp_Simple_IsAlive_Test_Explicit_Path()
    {
        // arrange
        var server = CreateServer(endpoint => endpoint.MapGraphQLHttp("/foo/bar"));

        // act
        var result = await server.GetAsync(
            new ClientQueryRequest { Query = "{ __typename }" },
            "/foo/bar");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Simple_IsAlive_Test_On_Non_GraphQL_Path()
    {
        // arrange
        var server = CreateStarWarsServer("/foo");

        // act
        var result = await server.GetAsync(
            new ClientQueryRequest { Query = "{ __typename }" },
            "/foo");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_GetHeroName()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    {
                        hero {
                            name
                        }
                    }"
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_GetHeroName_Casing_Is_Preserved()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    {
                        HERO: hero {
                            name
                        }
                    }"
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Double_Variable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                        query ($d: Float) {
                             double_arg(d: $d)
                        }",
                    Variables = new Dictionary<string, object?> { { "d", 1.539 } }
                },
                "/arguments");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Double_Max_Variable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                        query ($d: Float) {
                             double_arg(d: $d)
                        }",
                    Variables = new Dictionary<string, object?> { { "d", double.MaxValue } }
                },
                "/arguments");

        // assert
        new
        {
            double.MaxValue,
            result
        }.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Double_Min_Variable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                        query ($d: Float) {
                             double_arg(d: $d)
                        }",
                    Variables = new Dictionary<string, object?> { { "d", double.MinValue } }
                },
                "/arguments");

        // assert
        new
        {
            double.MinValue,
            result
        }.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Decimal_Max_Variable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                        query ($d: Decimal) {
                             decimal_arg(d: $d)
                        }",
                    Variables = new Dictionary<string, object?> { { "d", decimal.MaxValue } }
                },
                "/arguments");

        // assert
        new
        {
            decimal.MaxValue,
            result
        }.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Decimal_Min_Variable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query =
                        """
                        query ($d: Decimal) {
                             decimal_arg(d: $d)
                        }
                        """,
                    Variables = new Dictionary<string, object?>
                    {
                        { "d", decimal.MinValue }
                    }
                },
                "/arguments");

        // assert
        new
        {
            decimal.MinValue,
            result
        }.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_GetHeroName_With_EnumVariable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    query ($episode: Episode!) {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object?>
                    {
                        { "episode", "NEW_HOPE" }
                    }
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_GetHumanName_With_StringVariable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    query h($id: String!) {
                        human(id: $id) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object?>
                    {
                        { "id", "1000" }
                    }
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_CreateReviewForEpisode_With_ObjectVariable()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyServerOptions(o =>
                    o.AllowedGetOperations = AllowedGetOperations.QueryAndMutation));

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $review: ReviewInput!) {
                        createReview(episode: $ep, review: $review) {
                            stars
                            commentary
                        }
                    }",
                    Variables = new Dictionary<string, object?>
                    {
                        { "ep", "EMPIRE" },
                        {
                            "review",
                            new Dictionary<string, object?>
                            {
                                { "stars", 5 },
                                { "commentary", "This is a great movie!" }
                            }
                        }
                    }
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_CreateReviewForEpisode_Omit_NonNull_Variable()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyServerOptions(o =>
                    o.AllowedGetOperations = AllowedGetOperations.QueryAndMutation));

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $review: ReviewInput!) {
                        createReview(episode: $ep, review: $review) {
                            stars
                            commentary
                        }
                    }",
                    Variables = new Dictionary<string, object?>
                    {
                        {
                            "review",
                            new Dictionary<string, object?>
                            {
                                { "stars", 5 },
                                { "commentary", "This is a great movie!" }
                            }
                        }
                    }
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_CreateReviewForEpisode_Variables_In_ObjectValue()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyServerOptions(o =>
                    o.AllowedGetOperations = AllowedGetOperations.QueryAndMutation));

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $stars: Int!
                        $commentary: String!) {
                        createReview(episode: $ep, review: {
                            stars: $stars
                            commentary: $commentary
                        } ) {
                            stars
                            commentary
                        }
                    }",
                    Variables = new Dictionary<string, object?>
                    {
                        { "ep", "EMPIRE" },
                        { "stars", 5 },
                        { "commentary", "This is a great movie!" }
                    }
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_CreateReviewForEpisode_Variables_Unused()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    mutation CreateReviewForEpisode(
                        $ep: Episode!
                        $stars: Int!
                        $commentary: String!
                        $foo: Float) {
                        createReview(episode: $ep, review: {
                            stars: $stars
                            commentary: $commentary
                        } ) {
                            stars
                            commentary
                        }
                    }",
                    Variables = new Dictionary<string, object?>
                    {
                        { "ep", "EMPIRE" },
                        { "stars", 5 },
                        { "commentary", "This is a great movie!" }
                    }
                });

        // assert
        result.MatchSnapshot();
    }

    [InlineData("a")]
    [InlineData("b")]
    [Theory]
    public async Task SingleRequest_Execute_Specific_Operation(string operationName)
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    query a {
                        a: hero {
                            name
                        }
                    }

                    query b {
                        b: hero {
                            name
                        }
                    }",
                    OperationName = operationName
                });

        // assert
        result.MatchSnapshot(operationName);
    }

    [Fact]
    public async Task SingleRequest_ValidationError()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" } }
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_SyntaxError()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    {
                        ähero {
                            name
                        }
                    }"
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Mutation_ByDefault_NotAllowed_OnGet()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                        mutation CreateReviewForEpisode(
                            $ep: Episode!
                            $review: ReviewInput!) {
                            createReview(episode: $ep, review: $review) {
                                stars
                                commentary
                            }
                        }",
                    Variables = new Dictionary<string, object?>
                    {
                        { "ep", "EMPIRE" },
                        {
                            "review",
                            new Dictionary<string, object?>
                            {
                                { "stars", 5 },
                                { "commentary", "This is a great movie!" }
                            }
                        }
                    }
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Mutation_Set_To_Be_Allowed_on_Get()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyServerOptions(o =>
                    o.AllowedGetOperations = AllowedGetOperations.QueryAndMutation));

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                        mutation CreateReviewForEpisode(
                            $ep: Episode!
                            $review: ReviewInput!) {
                            createReview(episode: $ep, review: $review) {
                                stars
                                commentary
                            }
                        }",
                    Variables = new Dictionary<string, object?>
                    {
                        { "ep", "EMPIRE" },
                        {
                            "review",
                            new Dictionary<string, object?>
                            {
                                { "stars", 5 },
                                { "commentary", "This is a great movie!" }
                            }
                        }
                    }
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Get_Middleware_Is_Disabled()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyServerOptions(o =>
                {
                    o.EnableGetRequests = false;
                    o.Tool.Enable = false;
                }),
            configureConventions: e => e.WithOptions(o => o.Enable = false));

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    {
                        hero {
                            name
                        }
                    }"
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Get_ActivePersistedQuery()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await server.GetActivePersistedQueryAsync("md5Hash", "60ddx_GGk4FDObSa6eK0sg");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Get_ActivePersistedQuery_Invalid_Id_Format()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await server.GetActivePersistedQueryAsync("md5Hash", "60ddx/GGk4FDObSa6eK0sg==");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Get_ActivePersistedQuery_NotFound()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await server.GetActivePersistedQueryAsync("md5Hash", "abc");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Get_ActivePersistedQuery_AddQuery()
    {
        // arrange
        var server = CreateStarWarsServer();

        var document = Utf8GraphQLParser.Parse("{ __typename }");

        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);
        var hash = hashProvider.ComputeHash(
            Encoding.UTF8.GetBytes(document.ToString(false)));

        // act
        var resultA =
            await server.GetStoreActivePersistedQueryAsync(
                document.ToString(false),
                "md5Hash",
                hash.Value);

        var resultB = await server.GetActivePersistedQueryAsync("md5Hash", hash.Value);

        // assert
        new[]
        {
            resultA,
            resultB
        }.MatchSnapshot();
    }

    [Fact]
    public async Task Get_ActivePersistedQuery_AddQuery_Unformatted()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query = "{__typename}";

        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);
        var hash = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query));

        // act
        var resultA =
            await server.GetStoreActivePersistedQueryAsync(
                query,
                "md5Hash",
                hash.Value);

        var resultB = await server.GetActivePersistedQueryAsync("md5Hash", hash.Value);

        // assert
        new[] { resultA, resultB }.MatchSnapshot();
    }

    [Fact]
    public async Task Get_Should_ExecutePersistedOperation_When_DocumentIdIsSet()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var response = await client.GetAsync(
            "http://localhost:5000/graphql?documentId=60ddx_GGk4FDObSa6eK0sg",
            TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                Content-Length: 34
                -------------------------->
                Status Code: OK
                -------------------------->
                {"data":{"hero":{"name":"R2-D2"}}}
                """);
    }

    [Fact]
    public async Task Get_Should_RefuseVariableBatch_When_BatchingIsDisabled()
    {
        // arrange
        var listener = new RecordingListener();
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .AddDiagnosticEventListener(_ => listener)
                .ModifyServerOptions(o => o.Batching = AllowedBatching.None));
        var client = server.CreateClient();

        // act
        using var response = await client.GetAsync(
            "http://localhost:5000/graphql?query="
            + Uri.EscapeDataString("query($if: Boolean!) { hero { name @skip(if: $if) } }")
            + "&variables="
            + Uri.EscapeDataString("""[{ "if": true }, { "if": false }]"""),
            TestContext.Current.CancellationToken);

        // assert
        var diagnosticError = Assert.Single(listener.Errors);
        Assert.Equal(
            "Variable batching is disabled. Enable it with GraphQLServerOptions.Batching.",
            diagnosticError.Message);
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                Content-Length: 82
                -------------------------->
                Status Code: BadRequest
                -------------------------->
                {"errors":[{"message":"Invalid GraphQL Request.","extensions":{"code":"HC0009"}}]}
                """);
    }

    [Fact]
    public async Task Get_Should_RefuseVariableBatch_When_MaxBatchSizeIsExceeded()
    {
        // arrange
        var listener = new RecordingListener();
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .AddDiagnosticEventListener(_ => listener)
                .ModifyServerOptions(o => o.MaxBatchSize = 1));
        var client = server.CreateClient();

        // act
        using var response = await client.GetAsync(
            "http://localhost:5000/graphql?query="
            + Uri.EscapeDataString("query($if: Boolean!) { hero { name @skip(if: $if) } }")
            + "&variables="
            + Uri.EscapeDataString("""[{ "if": true }, { "if": false }]"""),
            TestContext.Current.CancellationToken);

        // assert
        var diagnosticError = Assert.Single(listener.Errors);
        Assert.Equal(
            "The batch size exceeds the maximum allowed batch size of 1.",
            diagnosticError.Message);
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                Content-Length: 117
                -------------------------->
                Status Code: BadRequest
                -------------------------->
                {"errors":[{"message":"The batch size exceeds the maximum allowed batch size of 1.","extensions":{"code":"HC0009"}}]}
                """);
    }

    [Fact]
    public async Task Throw_Custom_GraphQL_Error()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s.AddGraphQLServer()
                .AddHttpRequestInterceptor<ErrorRequestInterceptor>());

        // act
        var result =
            await server.GetAsync(
                new ClientQueryRequest
                {
                    Query =
                        """
                        {
                            hero {
                                name
                            }
                        }
                        """
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Get_Should_ReturnResult_When_QueriesOverPostAreOff()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureConventions: b => b.WithOptions(o => o.EnableQueriesOverPost = false));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "http://localhost:5000/graphql?query=%7B__typename%7D");
        request.Headers.Add("Accept", "application/graphql-response+json");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            """{"data":{"__typename":"Query"}}""",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private class ErrorRequestInterceptor : DefaultHttpRequestInterceptor
    {
        public override ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            throw new GraphQLException("MyCustomError");
        }
    }

    private sealed class RecordingListener : ServerDiagnosticEventListener
    {
        public List<IError> Errors { get; } = [];

        public override void HttpRequestError(HttpContext context, IError error)
            => Errors.Add(error);
    }
}

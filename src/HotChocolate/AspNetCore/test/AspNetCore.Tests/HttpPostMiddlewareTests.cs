using System.Net.Http.Json;
using CookieCrumble.HotChocolate.Formatters;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.Serialization.JsonNullIgnoreCondition;

namespace HotChocolate.AspNetCore;

public class HttpPostMiddlewareTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    private static readonly Uri _url = new("http://localhost:5000/graphql");

    [Fact]
    public async Task Simple_IsAlive_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = "{ __typename }", });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task LimitTokenCount_Success()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .ModifyParserOptions(o => o.MaxAllowedNodes = 6));

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = "{ s: __typename }", });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task LimitTokenCount_Fail()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .ModifyParserOptions(o => o.MaxAllowedNodes = 6));

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = "{ s: __typename t: __typename }", });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MapGraphQLHttp_Simple_IsAlive_Test()
    {
        // arrange
        var server = CreateServer(endpoint => endpoint.MapGraphQLHttp());

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = "{ __typename }", });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Serialize_Payload_With_Whitespaces()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: sc => sc.AddHttpResponseFormatter(indented: true));

        // act
        var result = await server.PostRawAsync(
            new ClientQueryRequest { Query = "{ __typename }", });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Serialize_Payload_Without_Extra_Whitespaces()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: sc => sc.AddHttpResponseFormatter(indented: false));

        // act
        var result = await server.PostRawAsync(
            new ClientQueryRequest { Query = "{ __typename }", });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Simple_IsAlive_Test_On_Non_GraphQL_Path()
    {
        // arrange
        var server = CreateStarWarsServer("/foo");

        // act
        var result = await server.PostAsync(
            new ClientQueryRequest { Query = "{ __typename }", },
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
            await server.PostAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    {
                        hero {
                            name
                        }
                    }",
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
            await server.PostAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    {
                        HERO: hero {
                            name
                        }
                    }",
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_GetHeroName_With_EnumVariable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    query ($episode: Episode!) {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" }, },
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Override_OnWriteResponseHeaders()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s.AddHttpResponseFormatter<CustomFormatter>());

        // act
        var result =
            await server.PostHttpAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    query ($episode: Episode!) {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" }, },
                });

        // assert
        result.MatchInlineSnapshot(
            """
            Headers:
            abc: def
            Content-Type: application/graphql-response+json; charset=utf-8
            -------------------------->
            Status Code: OK
            -------------------------->
            {"data":{"hero":{"name":"R2-D2"}}}
            """);
    }

    private class CustomFormatter : DefaultHttpResponseFormatter
    {
        protected override void OnWriteResponseHeaders(
            IOperationResult result,
            FormatInfo format,
            IHeaderDictionary headers)
        {
            headers.TryAdd("abc", "def");
        }
    }

    [Fact]
    public async Task SingleRequest_GetHumanName_With_StringVariable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    query h($id: String!) {
                        human(id: $id) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object?> { { "id", "1000" }, },
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Defer_Results()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostRawAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    {
                        ... @defer {
                            wait(m: 300)
                        }
                        hero(episode: NEW_HOPE)
                        {
                            name
                            ... on Droid @defer(label: ""my_id"")
                            {
                                id
                            }
                        }
                    }",
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Single_Diagnostic_Listener_Is_Triggered()
    {
        // arrange
        var listenerA = new TestListener();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .AddDiagnosticEventListener(_ => listenerA));

        // act
        await server.PostRawAsync(
            new ClientQueryRequest
            {
                Query = @"
                {
                    ... @defer {
                        wait(m: 300)
                    }
                    hero(episode: NEW_HOPE)
                    {
                        name
                        ... on Droid @defer(label: ""my_id"")
                        {
                            id
                        }
                    }
                }",
            });

        // assert
        Assert.True(listenerA.Triggered);
    }

    [Fact]
    public async Task Aggregate_Diagnostic_All_Listeners_Are_Triggered()
    {
        // arrange
        var listenerA = new TestListener();
        var listenerB = new TestListener();

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .AddDiagnosticEventListener(_ => listenerA)
                .AddDiagnosticEventListener(_ => listenerB));

        // act
        await server.PostRawAsync(
            new ClientQueryRequest
            {
                Query = @"
                {
                    ... @defer {
                        wait(m: 300)
                    }
                    hero(episode: NEW_HOPE)
                    {
                        name
                        ... on Droid @defer(label: ""my_id"")
                        {
                            id
                        }
                    }
                }",
            });

        // assert
        Assert.True(listenerA.Triggered);
        Assert.True(listenerB.Triggered);
    }

    [Fact]
    public async Task Ensure_Multipart_Format_Is_Correct_With_Defer()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostHttpAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    {
                        ... @defer {
                            wait(m: 300)
                        }
                        hero(episode: NEW_HOPE)
                        {
                            name
                            ... on Droid @defer(label: ""my_id"")
                            {
                                id
                            }
                        }
                    }",
                });

        // assert
        new GraphQLHttpResponse(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2",
                  "id": "2001"
                },
                "wait": true
              }
            }
            """);
    }

    [Fact]
    public async Task Ensure_Multipart_Format_Is_Correct_With_Defer_If_Condition_True()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostRawAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    query ($if: Boolean!){
                        ... @defer {
                            wait(m: 300)
                        }
                        hero(episode: NEW_HOPE)
                        {
                            name
                            ... on Droid @defer(label: ""my_id"", if: $if)
                            {
                                id
                            }
                        }
                    }",
                    Variables = new Dictionary<string, object?> { ["if"] = true, },
                });

        // assert
        result.Content.MatchSnapshot();
    }

    [Fact]
    public async Task Ensure_JSON_Format_Is_Correct_With_Defer_If_Condition_False()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostRawAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    query ($if: Boolean!){
                        hero(episode: NEW_HOPE)
                        {
                            name
                            ... on Droid @defer(label: ""my_id"", if: $if)
                            {
                                id
                            }
                        }
                    }",
                    Variables = new Dictionary<string, object?> { ["if"] = false, },
                });

        // assert
        result.Content.MatchSnapshot();
    }

    [Fact]
    public async Task Ensure_Multipart_Format_Is_Correct_With_Stream()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await server.PostHttpAsync(
            new ClientQueryRequest
            {
                Query = @"
                    {
                        ... @defer {
                            wait(m: 300)
                        }
                        hero(episode: NEW_HOPE)
                        {
                            name
                            friends(first: 10) {
                                nodes @stream(initialCount: 1 label: ""foo"") {
                                    name
                                }
                            }
                        }
                    }",
            });

        // assert
        new GraphQLHttpResponse(result).MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2",
                  "friends": {
                    "nodes": [
                      {
                        "name": "Luke Skywalker"
                      },
                      {
                        "name": "Han Solo"
                      },
                      {
                        "name": "Leia Organa"
                      }
                    ]
                  }
                },
                "wait": true
              }
            }
            """);
    }

    [Fact]
    public async Task SingleRequest_CreateReviewForEpisode_With_ObjectVariable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostAsync(
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
                            new Dictionary<string, object>
                            {
                                { "stars", 5 }, { "commentary", "This is a great movie!" },
                            }
                        },
                    },
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_CreateReviewForEpisode_Omit_NonNull_Variable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostAsync(
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
                                { "stars", 5 }, { "commentary", "This is a great movie!" },
                            }
                        },
                    },
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_CreateReviewForEpisode_Variables_In_ObjectValue()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostAsync(
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
                        { "commentary", "This is a great movie!" },
                    },
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
            await server.PostAsync(
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
                        { "commentary", "This is a great movie!" },
                    },
                });

        // assert
        result.MatchSnapshot();
    }

    [InlineData("a")]
    [InlineData("b")]
    [Theory]
    public async Task SingleRequest_Execute_Specific_Operation(
        string operationName)
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostAsync(
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
                    OperationName = operationName,
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
            await server.PostAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" }, },
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
            await server.PostAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    {
                        ähero {
                            name
                        }
                    }",
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
            await server.PostAsync(
                new ClientQueryRequest
                {
                    Query = @"
                            query ($d: Float) {
                                 double_arg(d: $d)
                            }",
                    Variables = new Dictionary<string, object?> { { "d", 1.539 }, },
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
            await server.PostAsync(
                new ClientQueryRequest
                {
                    Query = @"
                            query ($d: Float) {
                                 double_arg(d: $d)
                            }",
                    Variables = new Dictionary<string, object?> { { "d", double.MaxValue }, },
                },
                "/arguments");

        // assert
        new { double.MaxValue, result, }.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Double_Min_Variable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostAsync(
                new ClientQueryRequest
                {
                    Query = @"
                        query ($d: Float) {
                             double_arg(d: $d)
                        }",
                    Variables = new Dictionary<string, object?> { { "d", double.MinValue }, },
                },
                "/arguments");

        // assert
        new { double.MinValue, result, }.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Decimal_Max_Variable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostAsync(
                new ClientQueryRequest
                {
                    Query = @"
                            query ($d: Decimal) {
                                 decimal_arg(d: $d)
                            }",
                    Variables = new Dictionary<string, object?> { { "d", decimal.MaxValue }, },
                },
                "/arguments");

        // assert
        new { decimal.MaxValue, result, }.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Decimal_Min_Variable()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result =
            await server.PostAsync(
                new ClientQueryRequest
                {
                    Query = @"
                            query ($d: Decimal) {
                                 decimal_arg(d: $d)
                            }",
                    Variables = new Dictionary<string, object?> { { "d", decimal.MinValue }, },
                },
                "/arguments");

        // assert
        new { decimal.MinValue, result, }.MatchSnapshot();
    }

    [Fact]
    public async Task SingleRequest_Incomplete()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await server.PostAsync("{ \"query\":    ");

        // assert
        result.MatchSnapshot();
    }

    [InlineData("{}", 1)]
    [InlineData("{ }", 2)]
    [InlineData("{\n}", 3)]
    [InlineData("{\r\n}", 4)]
    [Theory]
    public async Task SingleRequest_Empty(string request, int id)
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await server.PostAsync(request);

        // assert
        result.MatchSnapshot(id);
    }

    [InlineData("[]", 1)]
    [InlineData("[ ]", 2)]
    [InlineData("[\n]", 3)]
    [InlineData("[\r\n]", 4)]
    [Theory]
    public async Task BatchRequest_Empty(string request, int id)
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await server.PostAsync(request);

        // assert
        result.MatchSnapshot(id);
    }

    [Fact]
    public async Task EmptyRequest()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var result = await server.PostAsync(string.Empty);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Ensure_Middleware_Mapping()
    {
        // arrange
        var server = CreateStarWarsServer("/foo");

        // act
        var result = await server.PostAsync(string.Empty);

        // assert
        result.MatchSnapshot();
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
            await server.PostAsync(
                new ClientQueryRequest
                {
                    Query = @"
                    {
                        hero {
                            name
                        }
                    }",
                });

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Strip_Null_Values_Variant_1()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s.AddHttpResponseFormatter(
                _ => new DefaultHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        Json = new JsonResultFormatterOptions
                        {
                            NullIgnoreCondition = Fields,
                        }
                    })));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __schema { description } }", }),
        };

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: application/json
        // expected status code: 200
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @"Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                {""data"":{""__schema"":{}}}");
    }

    [Fact]
    public async Task Strip_Null_Values_Variant_2()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s.AddHttpResponseFormatter(
                new HttpResponseFormatterOptions
                {
                    Json = new JsonResultFormatterOptions { NullIgnoreCondition = Fields, },
                }));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __schema { description } }", }),
        };

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: application/json
        // expected status code: 200
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @"Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                {""data"":{""__schema"":{}}}");
    }

    [Fact]
    public async Task Strip_Null_Elements()
    {
        // arrange
        var url = new Uri("http://localhost:5000/test");

        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer("test")
                .AddQueryType<NullListQuery>()
                .Services
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        Json = new JsonResultFormatterOptions { NullIgnoreCondition = Lists, },
                    }));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ nullValues }", }),
        };

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: application/json
        // expected status code: 200
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @"Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                {""data"":{""nullValues"":[""abc""]}}");
    }

    public class ErrorRequestInterceptor : DefaultHttpRequestInterceptor
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

    public class TestListener : ServerDiagnosticEventListener
    {
        public bool Triggered { get; set; }

        public override IDisposable ExecuteHttpRequest(HttpContext context, HttpRequestKind kind)
        {
            Triggered = true;
            return EmptyScope;
        }
    }

    public class NullListQuery
    {
        public List<string?> NullValues => [null, "abc", null,];
    }
}

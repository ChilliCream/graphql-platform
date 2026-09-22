using System.Net;
using System.Text;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.AspNetCore.Tests.Utilities.TestServerExtensions;

namespace HotChocolate.AspNetCore;

public class HttpQueryMiddlewareTests(TestServerFactory serverFactory)
    : ServerTestBase(serverFactory)
{
    private static readonly HttpMethod s_query = new("QUERY");

    [Fact]
    public async Task Query_Should_ReturnResult_When_QueryRequestsAreEnabled()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureConventions: b => b.WithOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = CreateQueryRequest("""{ "query": "{ __typename }" }""");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                {"data":{"__typename":"Query"}}
                """);
    }

    [Fact]
    public async Task Query_Should_ReturnResult_When_VariablesAreSent()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureConventions: b => b.WithOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = CreateQueryRequest(
            """
            {
              "query": "query($episode: Episode!) { hero(episode: $episode) { name } }",
              "variables": { "episode": "JEDI" }
            }
            """);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                {"data":{"hero":{"name":"R2-D2"}}}
                """);
    }

    [Fact]
    public async Task Query_Should_ReturnNotFound_When_QueryRequestsAreDisabled()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = CreateQueryRequest("""{ "query": "{ __typename }" }""");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Query_Should_ReturnResult_When_EnabledBySchemaOptions()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .ModifyServerOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = CreateQueryRequest("""{ "query": "{ __typename }" }""");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Query_Should_ReturnNotFound_When_DisabledPerEndpoint()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .ModifyServerOptions(o => o.EnableQueryRequests = true),
            configureConventions: b => b.WithOptions(o => o.EnableQueryRequests = false));
        var client = server.CreateClient();

        // act
        using var request = CreateQueryRequest("""{ "query": "{ __typename }" }""");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Query_Should_ReturnResult_When_ExplicitHttpEndpointIsMapped()
    {
        // arrange
        var server = CreateServer(
            endpoints => endpoints
                .MapGraphQLHttp()
                .WithOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = CreateQueryRequest("""{ "query": "{ __typename }" }""");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Query_Should_ReturnBadRequest_When_BodyIsArray()
    {
        // arrange
        // the StarWars server allows every batching mode, so the refusal is QUERY's own.
        var listener = new RecordingListener();
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .AddDiagnosticEventListener(_ => listener),
            configureConventions: b => b.WithOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = CreateQueryRequest(
            """
            [
              { "query": "{ hero(episode: NEW_HOPE) { name } }" },
              { "query": "{ hero(episode: EMPIRE) { name } }" }
            ]
            """);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        var diagnosticError = Assert.Single(listener.Errors);
        Assert.Equal(
            "Request batching is not supported for HTTP QUERY requests.",
            diagnosticError.Message);
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: BadRequest
                -------------------------->
                {"errors":[{"message":"Invalid GraphQL Request.","extensions":{"code":"HC0009"}}]}
                """);
    }

    [Fact]
    public async Task Query_Should_RunRequestInterceptor_When_RequestIsCreated()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .AddHttpRequestInterceptor<ErrorRequestInterceptor>(),
            configureConventions: b => b.WithOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = CreateQueryRequest("""{ "query": "{ __typename }" }""");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: BadRequest
                -------------------------->
                {"errors":[{"message":"MyCustomError"}]}
                """);
    }

    private static HttpRequestMessage CreateQueryRequest(string body, string path = "/graphql")
    {
        return new HttpRequestMessage(s_query, CreateUrl(path))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private sealed class ErrorRequestInterceptor : DefaultHttpRequestInterceptor
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

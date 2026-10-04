using System.Net;
#if !NET11_0_OR_GREATER
using System.Net.Http.Json;
#endif
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public class PersistedOperationMiddlewareTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    private static readonly HttpMethod s_queryMethod = new("QUERY");

    [Fact]
    public async Task ExecutePersistedOperation_Success()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var result = await client.GetAsync(
            "/graphql/persisted/60ddx_GGk4FDObSa6eK0sg/GetHeroName",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        json!.RootElement.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_NotFound()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var result = await client.GetAsync(
            "/graphql/persisted/60ddx_GGk4FDObSa6eK0s1/GetHeroName",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        json!.RootElement.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_InvalidId()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var result = await client.GetAsync(
            "/graphql/persisted/60ddx_GG+k4FDObSa6eK0s1/GetHeroName",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        json!.RootElement.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_HttpPost_Empty_Body_Success()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var body = new StringContent(
            """
            {
            }
            """,
            Encoding.UTF8,
            "application/json");

        var result = await client.PostAsync(
            "/graphql/persisted/60ddx_GGk4FDObSa6eK0sg/GetHeroName",
            body,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        json!.RootElement.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_Require_OperationName_Fail()
    {
        // arrange
        var server = CreateStarWarsServer(requireOperationName: true);
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var body = new StringContent(
            """
            {
            }
            """,
            Encoding.UTF8,
            "application/json");

        var result = await client.PostAsync(
            "/graphql/persisted/60ddx_GGk4FDObSa6eK0sg",
            body,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        json!.RootElement.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_OperationName_Is_Optional_Success()
    {
        // arrange
        var server = CreateStarWarsServer(requireOperationName: false);
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var body = new StringContent(
            """
            {
            }
            """,
            Encoding.UTF8,
            "application/json");

        var result = await client.PostAsync(
            "/graphql/persisted/60ddx_GGk4FDObSa6eK0sg",
            body,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        json!.RootElement.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_HttpPost_Empty_Body_NotFound()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var body = new StringContent(
            """
            {
            }
            """,
            Encoding.UTF8,
            "application/json");

        var result = await client.PostAsync(
            "/graphql/persisted/60ddx_GGk4FDObSa6eK0sg1/GetHeroName",
            body,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        json!.RootElement.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_HttpPost_Empty_Body_InvalidId()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var body = new StringContent(
            """
            {
            }
            """,
            Encoding.UTF8,
            "application/json");

        var result = await client.PostAsync(
            "/graphql/persisted/60ddx_GGk4+FDObSa6eK0sg1/GetHeroName",
            body,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        json!.RootElement.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ExecutePersistedOperation_HttpPost_With_Variables_Success()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var body = new StringContent(
            """
            {
                "variables": {
                    "if": false
                }
            }
            """,
            Encoding.UTF8,
            "application/json");

        var result = await client.PostAsync(
            "/graphql/persisted/abc123/Test",
            body,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        json!.RootElement.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Query_Should_ExecutePersistedOperation_When_QueryRequestsAreEnabled()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .ModifyServerOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            s_queryMethod,
            "http://localhost:5000/graphql/persisted/60ddx_GGk4FDObSa6eK0sg/GetHeroName");
        request.Content = new StringContent("{ }", Encoding.UTF8, "application/json");

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
    public async Task Query_Should_ApplyVariables_When_BodyCarriesVariables()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .ModifyServerOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            s_queryMethod,
            "http://localhost:5000/graphql/persisted/abc123");
        request.Content = new StringContent(
            """{ "variables": { "if": true } }""",
            Encoding.UTF8,
            "application/json");

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
                {"data":{"hero":{}}}
                """);
    }

    [Fact]
    public async Task Query_Should_ReturnUnprocessableContent_When_PersistedMutationIsSent()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .ModifyServerOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            s_queryMethod,
            "http://localhost:5000/graphql/persisted/createReview/CreateReview");
        request.Content = new StringContent("{ }", Encoding.UTF8, "application/json");

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
                Status Code: UnprocessableEntity
                -------------------------->
                {"errors":[{"message":"The specified operation kind is not allowed."}]}
                """);
    }

    [Fact]
    public async Task Query_Should_ReturnBadRequest_When_VariablesIsArray()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .ModifyServerOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            s_queryMethod,
            "http://localhost:5000/graphql/persisted/abc123");
        request.Content = new StringContent(
            """{ "variables": [{ "if": true }, { "if": false }] }""",
            Encoding.UTF8,
            "application/json");

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
                {"errors":[{"message":"Invalid GraphQL Request.","extensions":{"code":"HC0009"}}]}
                """);
    }

    // Without the option the persisted path maps no QUERY route, and the request is answered
    // like any method the path does not map.
    [Fact]
    public async Task Query_Should_ReturnNotFound_When_QueryRequestsAreDisabled()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            s_queryMethod,
            "http://localhost:5000/graphql/persisted/60ddx_GGk4FDObSa6eK0sg/GetHeroName");
        request.Content = new StringContent("{ }", Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(response.Content.Headers.Allow);
    }
}

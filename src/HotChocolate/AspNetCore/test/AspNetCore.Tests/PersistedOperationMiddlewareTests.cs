using System.Net;
#if !NET11_0_OR_GREATER
using System.Net.Http.Json;
#endif
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public class PersistedOperationMiddlewareTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    private const string OperationIdUrl =
        "http://localhost:5000/graphql/persisted/60ddx_GGk4FDObSa6eK0sg";
    private const string OperationNameUrl = OperationIdUrl + "/GetHeroName";

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
            "http://localhost:5000/graphql/persisted/abc123?variables="
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
            "http://localhost:5000/graphql/persisted/abc123?variables="
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
    public async Task Post_Should_RefuseVariableBatch_When_BatchingIsDisabled()
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
        using var response = await client.PostAsync(
            "http://localhost:5000/graphql/persisted/abc123",
            new StringContent(
                """{ "variables": [{ "if": true }, { "if": false }] }""",
                Encoding.UTF8,
                "application/json"),
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
    public async Task Post_Should_RefuseVariableBatch_When_MaxBatchSizeIsExceeded()
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
        using var response = await client.PostAsync(
            "http://localhost:5000/graphql/persisted/abc123",
            new StringContent(
                """{ "variables": [{ "if": true }, { "if": false }] }""",
                Encoding.UTF8,
                "application/json"),
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

    [Theory]
    [InlineData(HttpTransportVersion.Legacy, "text/plain", HttpStatusCode.NotFound)]
    [InlineData(HttpTransportVersion.Legacy, null, HttpStatusCode.NotFound)]
    [InlineData(HttpTransportVersion.Draft20250508, "text/plain", HttpStatusCode.NotFound)]
    [InlineData(HttpTransportVersion.Draft20250508, null, HttpStatusCode.NotFound)]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        "text/plain",
        HttpStatusCode.UnsupportedMediaType)]
    [InlineData(HttpTransportVersion.Draft20260903, null, HttpStatusCode.UnsupportedMediaType)]
    public async Task Post_Should_ReturnUnsupportedMediaType_When_ContentTypeIsNotJson(
        HttpTransportVersion transportVersion,
        string? contentType,
        HttpStatusCode expectedStatusCode)
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = transportVersion
                    }));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "http://localhost:5000/graphql/persisted/60ddx_GGk4FDObSa6eK0sg/GetHeroName");
        request.Content = CreateContent("{ }", contentType);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(
            "",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
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
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                Content-Length: 20
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
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                Content-Length: 71
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
                Vary: Accept
                Content-Type: application/graphql-response+json; charset=utf-8
                Content-Length: 82
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

    [Theory]
    [InlineData(HttpTransportVersion.Legacy, "text/plain", HttpStatusCode.NotFound, null)]
    [InlineData(HttpTransportVersion.Legacy, null, HttpStatusCode.NotFound, null)]
    [InlineData(HttpTransportVersion.Draft20250508, "text/plain", HttpStatusCode.NotFound, null)]
    [InlineData(HttpTransportVersion.Draft20250508, null, HttpStatusCode.NotFound, null)]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        "text/plain",
        HttpStatusCode.UnsupportedMediaType,
        "application/json")]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        null,
        HttpStatusCode.UnsupportedMediaType,
        "application/json")]
    public async Task Query_Should_ReturnUnsupportedMediaType_When_ContentTypeIsNotJson(
        HttpTransportVersion transportVersion,
        string? contentType,
        HttpStatusCode expectedStatusCode,
        string? expectedAcceptQuery)
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = transportVersion
                    })
                .ModifyServerOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            s_queryMethod,
            "http://localhost:5000/graphql/persisted/60ddx_GGk4FDObSa6eK0sg/GetHeroName");
        request.Content = CreateContent("{ }", contentType);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(
            expectedAcceptQuery,
            response.Headers.TryGetValues("Accept-Query", out var acceptQuery)
                ? string.Join(", ", acceptQuery)
                : null);
        Assert.Equal(
            "",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("QUERY")]
    public async Task Request_Should_ReturnVaryAccept_When_PersistedOperationIsExecuted(
        string method)
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .ModifyServerOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            "http://localhost:5000/graphql/persisted/60ddx_GGk4FDObSa6eK0sg/GetHeroName");

        if (method is not "GET")
        {
            request.Content = new StringContent("{ }", Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["Accept"], response.Headers.Vary);
    }

    // A method the persisted routes do not map is answered as on the GraphQL endpoint, without a
    // response body.
    [Theory]
    [InlineData(
        HttpTransportVersion.Legacy,
        "PUT",
        OperationNameUrl,
        HttpStatusCode.NotFound,
        new string[0])]
    [InlineData(
        HttpTransportVersion.Draft20250508,
        "PUT",
        OperationIdUrl,
        HttpStatusCode.NotFound,
        new string[0])]
    [InlineData(
        HttpTransportVersion.Draft20250508,
        "PUT",
        OperationNameUrl,
        HttpStatusCode.NotFound,
        new string[0])]
    [InlineData(
        HttpTransportVersion.Draft20250508,
        "HEAD",
        OperationNameUrl,
        HttpStatusCode.NotFound,
        new string[0])]
    [InlineData(
        HttpTransportVersion.Draft20250508,
        "QUERY",
        OperationNameUrl,
        HttpStatusCode.NotFound,
        new string[0])]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        "PUT",
        OperationIdUrl,
        HttpStatusCode.MethodNotAllowed,
        new[] { "GET", "OPTIONS", "POST" })]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        "PUT",
        OperationNameUrl,
        HttpStatusCode.MethodNotAllowed,
        new[] { "GET", "OPTIONS", "POST" })]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        "DELETE",
        OperationNameUrl,
        HttpStatusCode.MethodNotAllowed,
        new[] { "GET", "OPTIONS", "POST" })]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        "HEAD",
        OperationNameUrl,
        HttpStatusCode.MethodNotAllowed,
        new[] { "GET", "OPTIONS", "POST" })]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        "QUERY",
        OperationNameUrl,
        HttpStatusCode.MethodNotAllowed,
        new[] { "GET", "OPTIONS", "POST" })]
    public async Task Request_Should_ReturnMethodNotAllowed_When_MethodIsNotMapped(
        HttpTransportVersion transportVersion,
        string method,
        string url,
        HttpStatusCode expectedStatusCode,
        string[] expectedAllow)
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = transportVersion
                    }));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (method is not "HEAD")
        {
            request.Content = new StringContent("{ }", Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(expectedAllow, response.Content.Headers.Allow);
        Assert.False(response.Headers.Contains("Accept-Query"));
        Assert.Equal(
            "",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(
        HttpTransportVersion.Legacy,
        OperationNameUrl,
        HttpStatusCode.NotFound,
        new string[0])]
    [InlineData(
        HttpTransportVersion.Draft20250508,
        OperationIdUrl,
        HttpStatusCode.NotFound,
        new string[0])]
    [InlineData(
        HttpTransportVersion.Draft20250508,
        OperationNameUrl,
        HttpStatusCode.NotFound,
        new string[0])]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        OperationIdUrl,
        HttpStatusCode.NoContent,
        new[] { "GET", "OPTIONS", "POST" })]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        OperationNameUrl,
        HttpStatusCode.NoContent,
        new[] { "GET", "OPTIONS", "POST" })]
    public async Task Options_Should_ReturnAllowedMethods_When_PersistedRouteIsRequested(
        HttpTransportVersion transportVersion,
        string url,
        HttpStatusCode expectedStatusCode,
        string[] expectedAllow)
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = transportVersion
                    }));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Options, url);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(expectedAllow, response.Content.Headers.Allow);
    }

    [Theory]
    [InlineData("PUT", HttpStatusCode.MethodNotAllowed)]
    [InlineData("OPTIONS", HttpStatusCode.NoContent)]
    public async Task Request_Should_ListQuery_When_QueryRequestsAreEnabled(
        string method,
        HttpStatusCode expectedStatusCode)
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = HttpTransportVersion.Draft20260903
                    })
                .ModifyServerOptions(o => o.EnableQueryRequests = true));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(new HttpMethod(method), OperationNameUrl);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(["GET", "OPTIONS", "POST", "QUERY"], response.Content.Headers.Allow);
        Assert.Equal(["application/json"], response.Headers.GetValues("Accept-Query"));
    }

    // The answer does not depend on whether a GraphQL endpoint is mapped above the persisted path.
    [Theory]
    [InlineData(HttpTransportVersion.Draft20250508, HttpStatusCode.NotFound, new string[0])]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        HttpStatusCode.MethodNotAllowed,
        new[] { "GET", "OPTIONS", "POST" })]
    public async Task Put_Should_ReturnMethodNotAllowed_When_PersistedRoutesAreMappedAlone(
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode,
        string[] expectedAllow)
    {
        // arrange
        var server = ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType(d => d.Field("greeting").Type<StringType>().Resolve("Hello"))
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = transportVersion
                    }),
            app => app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQLPersistedOperations()));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Put, OperationNameUrl);
        request.Content = new StringContent("{ }", Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(expectedAllow, response.Content.Headers.Allow);
    }

    [Theory]
    [InlineData(HttpTransportVersion.Draft20250508, HttpStatusCode.NotFound, new string[0])]
    [InlineData(
        HttpTransportVersion.Draft20260903,
        HttpStatusCode.NoContent,
        new[] { "GET", "OPTIONS", "POST" })]
    public async Task Options_Should_ReturnAllowedMethods_When_PersistedRoutesAreMappedAlone(
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode,
        string[] expectedAllow)
    {
        // arrange
        var server = ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType(d => d.Field("greeting").Type<StringType>().Resolve("Hello"))
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = transportVersion
                    }),
            app => app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQLPersistedOperations()));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Options, OperationNameUrl);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(expectedAllow, response.Content.Headers.Allow);
    }

    // A CORS preflight that a policy covers is answered by the CORS middleware.
    [Fact]
    public async Task Options_Should_ReturnCorsPreflight_When_CorsPolicyCoversPersistedRoute()
    {
        // arrange
        var server = ServerFactory.Create(
            services => services
                .AddCors(o => o.AddDefaultPolicy(
                    p => p.WithOrigins("https://example.com").WithMethods("POST")))
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType(d => d.Field("greeting").Type<StringType>().Resolve("Hello"))
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = HttpTransportVersion.Draft20260903
                    }),
            app => app
                .UseRouting()
                .UseCors()
                .UseEndpoints(
                    endpoints =>
                    {
                        endpoints.MapGraphQLPersistedOperations();
                        endpoints.MapGraphQL();
                    }));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Options, OperationNameUrl);
        request.Headers.Add("Origin", "https://example.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(
            ["https://example.com"],
            response.Headers.GetValues("Access-Control-Allow-Origin"));
        Assert.Equal(["POST"], response.Headers.GetValues("Access-Control-Allow-Methods"));
        Assert.Empty(response.Content.Headers.Allow);
    }

    private static HttpContent CreateContent(string body, string? contentType)
        => contentType is null
            ? new ByteArrayContent(Encoding.UTF8.GetBytes(body))
            : new StringContent(body, Encoding.UTF8, contentType);

    private sealed class RecordingListener : ServerDiagnosticEventListener
    {
        public List<IError> Errors { get; } = [];

        public override void HttpRequestError(HttpContext context, IError error)
            => Errors.Add(error);
    }
}

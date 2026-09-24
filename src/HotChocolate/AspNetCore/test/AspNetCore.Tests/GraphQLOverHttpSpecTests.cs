using System.Net;
using System.Net.Http.Json;
using System.Text;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Features;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using static System.Net.Http.HttpCompletionOption;
using static System.Net.HttpStatusCode;
using static HotChocolate.AspNetCore.HttpTransportVersion;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;
using OperationInfo = HotChocolate.Execution.Pipeline.OperationInfo;
using WellKnownRequestMiddleware = HotChocolate.Execution.WellKnownRequestMiddleware;

namespace HotChocolate.AspNetCore;

public class GraphQLOverHttpSpecTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    private static readonly Uri s_url = new("http://localhost:5000/graphql");

    [Theory]
    [InlineData(null, Latest, ContentType.GraphQLResponse)]
    [InlineData(null, Legacy, ContentType.Json)]
    [InlineData("*/*", Latest, ContentType.GraphQLResponse)]
    [InlineData("*/*", Legacy, ContentType.Json)]
    [InlineData("application/*", Latest, ContentType.GraphQLResponse)]
    [InlineData("application/*", Legacy, ContentType.Json)]
    [InlineData("application/json, */*", Latest, ContentType.GraphQLResponse)]
    [InlineData("application/json, */*", Legacy, ContentType.Json)]
    [InlineData("application/json, application/*", Latest, ContentType.GraphQLResponse)]
    [InlineData("application/json, application/*", Legacy, ContentType.Json)]
    [InlineData("application/json, text/plain, */*", Latest, ContentType.GraphQLResponse)]
    [InlineData("application/json, text/plain, */*", Legacy, ContentType.Json)]
    [InlineData(ContentType.Json, Latest, ContentType.Json)]
    [InlineData(ContentType.Json, Legacy, ContentType.Json)]
    [InlineData(ContentType.GraphQLResponse, Latest, ContentType.GraphQLResponse)]
    [InlineData(ContentType.GraphQLResponse, Legacy, ContentType.GraphQLResponse)]
    [InlineData("application/graphql-response+json; charset=utf-8, multipart/mixed; charset=utf-8",
            Latest, ContentType.GraphQLResponse)]
    [InlineData("application/graphql-response+json; charset=utf-8, multipart/mixed; charset=utf-8",
            Legacy, ContentType.GraphQLResponse)]
    [InlineData("application/graphql-response+json, multipart/mixed", Latest, ContentType.GraphQLResponse)]
    [InlineData("application/graphql-response+json, multipart/mixed", Legacy, ContentType.GraphQLResponse)]
    [InlineData("multipart/mixed,application/graphql-response+json", Latest, ContentType.GraphQLResponse)]
    [InlineData("multipart/mixed,application/graphql-response+json", Legacy, ContentType.GraphQLResponse)]
    [InlineData("text/event-stream, multipart/mixed,application/json, application/graphql-response+json",
            Latest, ContentType.GraphQLResponse)]
    [InlineData("text/event-stream, multipart/mixed,application/json, application/graphql-response+json",
            Legacy, ContentType.GraphQLResponse)]
    [InlineData("application/graphql-response+json; charset=utf-8, application/json; charset=utf-8",
            Latest, ContentType.GraphQLResponse)]
    [InlineData("application/graphql-response+json; charset=utf-8, application/json; charset=utf-8",
            Legacy, ContentType.GraphQLResponse)]
    public async Task SingleResult_Success(string? acceptHeader, HttpTransportVersion transportVersion,
        string expectedContentType)
    {
        // arrange
        var client = GetClient(transportVersion);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @$"Headers:
                Content-Type: {expectedContentType}
                -------------------------->
                Status Code: OK
                -------------------------->
                "
                + @"{""data"":{""__typename"":""Query""}}");
    }

    [Theory]
    [InlineData("multipart/mixed")]
    [InlineData("multipart/*")]
    public async Task SingleResult_MultipartAcceptHeader(string acceptHeader)
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });
        request.Headers.Add("Accept", acceptHeader);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Content-Type: multipart/mixed; boundary="-"
                -------------------------->
                Status Code: OK
                -------------------------->

                ---
                Content-Type: application/json; charset=utf-8

                {"data":{"__typename":"Query"}}
                -----

                """);
    }

    [Theory]
    [InlineData(null, Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData(null, Legacy, OK, ContentType.Json)]
    [InlineData("*/*", Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData("*/*", Legacy, OK, ContentType.Json)]
    [InlineData("application/*", Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData("application/*", Legacy, OK, ContentType.Json)]
    [InlineData(ContentType.Json, Latest, BadRequest, ContentType.Json)]
    [InlineData(ContentType.Json, Legacy, OK, ContentType.Json)]
    [InlineData(ContentType.GraphQLResponse, Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData(ContentType.GraphQLResponse, Legacy, BadRequest, ContentType.GraphQLResponse)]
    public async Task Query_No_Body(string? acceptHeader, HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode, string expectedContentType)
    {
        // arrange
        var client = GetClient(transportVersion);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = new ByteArrayContent([])
        {
            Headers =
            {
                ContentType = new MediaTypeHeaderValue("application/json")
                {
                    CharSet = "utf-8"
                }
            }
        };
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                $$$"""
                Headers:
                Content-Type: {{{expectedContentType}}}
                -------------------------->
                Status Code: {{{expectedStatusCode}}}
                -------------------------->
                {"errors":[{"message":"Invalid JSON document.","extensions":{"code":"HC0012"}}]}
                """);
    }

    [Theory]
    [InlineData(null, Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData(null, Legacy, OK, ContentType.Json)]
    [InlineData("*/*", Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData("*/*", Legacy, OK, ContentType.Json)]
    [InlineData("application/*", Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData("application/*", Legacy, OK, ContentType.Json)]
    [InlineData(ContentType.Json, Latest, OK, ContentType.Json)]
    [InlineData(ContentType.Json, Legacy, OK, ContentType.Json)]
    [InlineData(ContentType.GraphQLResponse, Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData(ContentType.GraphQLResponse, Legacy, BadRequest, ContentType.GraphQLResponse)]
    public async Task ValidationError(string? acceptHeader, HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode, string expectedContentType)
    {
        // arrange
        var client = GetClient(transportVersion);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typ$ename }" });
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @$"Headers:
                Content-Type: {expectedContentType}
                -------------------------->
                Status Code: {expectedStatusCode}
                -------------------------->
                "
                + @"{""errors"":[{""message"":""Expected a `Name`-token, but found a "
                + @"`Dollar`-token."",""locations"":[{""line"":1,""column"":8}],"
                + @"""extensions"":{""code"":""HC0011""}}]}");
    }

    [Theory]
    [InlineData(null, Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData(null, Legacy, OK, ContentType.Json)]
    [InlineData("*/*", Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData("*/*", Legacy, OK, ContentType.Json)]
    [InlineData("application/*", Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData("application/*", Legacy, OK, ContentType.Json)]
    [InlineData(ContentType.Json, Latest, OK, ContentType.Json)]
    [InlineData(ContentType.Json, Legacy, OK, ContentType.Json)]
    [InlineData(ContentType.GraphQLResponse, Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData(ContentType.GraphQLResponse, Legacy, BadRequest, ContentType.GraphQLResponse)]
    public async Task ValidationError2(string? acceptHeader, HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode, string expectedContentType)
    {
        // arrange
        var client = GetClient(transportVersion);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __type name }" });
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            expectedContentType,
            response.Content.Headers.GetValues("Content-Type").Single());
        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Fact]
    public async Task UnsupportedAcceptHeaderValue()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });
        request.Headers.TryAddWithoutValidation("Accept", "unsupported");

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
                {"errors":[{"message":"Unable to parse the accept header value `unsupported`.","extensions":{"code":"HC0064","headerValue":"unsupported"}}]}
                """);
    }

    [Fact]
    public async Task UnsupportedApplicationAcceptHeaderValue()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });

        request.Headers.TryAddWithoutValidation("Accept", "application/unsupported");

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
                Status Code: NotAcceptable
                -------------------------->
                {"errors":[{"message":"None of the `Accept` header values is supported.","extensions":{"code":"HC0063"}}]}
                """);
    }

    [Fact]
    public async Task EventStream_Sends_KeepAlive()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(
            new ClientQueryRequest
            {
                Query = "subscription {delay(count: 2, delay:15000)}"
            });
        request.Headers.Add("Accept", "text/event-stream");

        using var response = await client.SendAsync(
            request,
            ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Cache-Control: no-cache
                Content-Type: text/event-stream; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                event: next
                data: {"data":{"delay":"next"}}

                :

                event: next
                data: {"data":{"delay":"next"}}

                :

                event: complete


                """);
    }

    [Fact]
    public async Task EventStream_When_Accept_Is_All()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(
            new ClientQueryRequest
            {
                Query = "subscription {delay(count: 2, delay:15000)}"
            });
        request.Headers.Add("Accept", "*/*");

        using var response = await client.SendAsync(
            request,
            ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Cache-Control: no-cache
                Content-Type: text/event-stream; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                event: next
                data: {"data":{"delay":"next"}}

                :

                event: next
                data: {"data":{"delay":"next"}}

                :

                event: complete


                """);
    }

    [Fact]
    public async Task EventStream_When_Accept_Is_All_And_Subscription_Directive()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(
            new ClientQueryRequest
            {
                Query = "subscription foo @foo(bar: 1) {delay(count: 2, delay:15000)}"
            });
        request.Headers.Add("Accept", "*/*");

        using var response = await client.SendAsync(
            request,
            ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                """
                Headers:
                Cache-Control: no-cache
                Content-Type: text/event-stream; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                event: next
                data: {"data":{"delay":"next"}}

                :

                event: next
                data: {"data":{"delay":"next"}}

                :

                event: complete


                """);
    }

    [Fact]
    public async Task OperationBatch()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = new DefaultGraphQLHttpClient(server.CreateClient());
        var snapshot = new Snapshot();

        // act
        var request = new GraphQLHttpRequest(
            new OperationBatchRequest(
            [
                new OperationRequest(
                    """
                    {
                        hero(episode: NEW_HOPE) {
                            name
                        }
                    }
                    """),
                new OperationRequest(
                    """
                    {
                        hero(episode: EMPIRE) {
                            name
                        }
                    }
                    """)
            ]),
            new Uri("http://localhost:5000/graphql"));

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(OK, response.StatusCode);

        var sortedResults = new SortedList<(int?, int?), OperationResult>();

        await foreach (var result in response.ReadAsResultStreamAsync())
        {
            sortedResults.Add((result.RequestIndex, result.VariableIndex), result);
        }

        foreach (var result in sortedResults.Values)
        {
            snapshot.Add(result);
        }

        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task VariableBatch()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = new DefaultGraphQLHttpClient(server.CreateClient());
        var snapshot = new Snapshot();

        // act
        var request = new GraphQLHttpRequest(
            new VariableBatchRequest(
                """
                query($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }
                """,
                variables:
                [
                    new Dictionary<string, object?> { { "episode", "NEW_HOPE" } },
                    new Dictionary<string, object?> { { "episode", "EMPIRE" } }
                ]),
            new Uri("http://localhost:5000/graphql"));

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(OK, response.StatusCode);

        await foreach (var result in response.ReadAsResultStreamAsync())
        {
            snapshot.Add(result);
        }

        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task After_Execution_StatusCode_Is_200()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = new DefaultGraphQLHttpClient(server.CreateClient());

        // act
        var request = new GraphQLHttpRequest(
            new OperationRequest("{ error }"),
            new Uri("http://localhost:5000/notnull"));

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(OK, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_OnError_Value_Returns_BadRequest()
    {
        // arrange
        var client = GetClient(Latest);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = new StringContent(
            """{"query":"{ __typename }","onError":"HALT"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("onError", body, StringComparison.OrdinalIgnoreCase);
    }

    // A pipeline step that finds the request state an earlier step provides missing is a failure
    // inside the server, and is answered 500 like one.
    [Theory]
    [InlineData("""{ "query": "{ __typename }" }""", Latest)]
    [InlineData("""{ "id": "60ddx_GGk4FDObSa6eK0sg" }""", Latest)]
    public async Task Post_Should_ReturnInternalServerError_When_DocumentIsMissingBeforeValidation(
        string body,
        HttpTransportVersion transportVersion)
    {
        // arrange
        var client = GetClient(
            transportVersion,
            WellKnownRequestMiddleware.DocumentValidationMiddleware,
            context => context.OperationDocumentInfo.Document = null);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

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
                Status Code: InternalServerError
                -------------------------->
                {"errors":[{"message":"The query request contains no document or no document id.","extensions":{"code":"HC0015"}}]}
                """);
    }

    [Theory]
    [InlineData(Latest)]
    public async Task Post_Should_ReturnInternalServerError_When_CachedDocumentIsMissing(
        HttpTransportVersion transportVersion)
    {
        // arrange
        // the first request puts its document into the document cache under the document ID
        var client = GetClient(
            transportVersion,
            WellKnownRequestMiddleware.DocumentValidationMiddleware,
            context =>
            {
                if (context.OperationDocumentInfo.IsCached)
                {
                    context.OperationDocumentInfo.Document = null;
                }
            });

        using var cacheRequest = new HttpRequestMessage(HttpMethod.Post, s_url);
        cacheRequest.Content = new StringContent(
            """{ "id": "cached-document", "query": "{ __typename }" }""",
            Encoding.UTF8,
            "application/json");
        using var cacheResponse = await client.SendAsync(
            cacheRequest,
            TestContext.Current.CancellationToken);
        Assert.Equal(OK, cacheResponse.StatusCode);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = new StringContent(
            """{ "id": "cached-document" }""",
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
                Status Code: InternalServerError
                -------------------------->
                {"errors":[{"message":"The query request contains no document or no document id.","extensions":{"code":"HC0015"}}]}
                """);
    }

    // A request that carries only a document ID, sent to a pipeline without persisted
    // operations, is a request error.
    [Theory]
    [InlineData(Latest, BadRequest)]
    public async Task Post_Should_ReturnBadRequest_When_DocumentIdIsNotResolved(
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode)
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer("test")
                .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = transportVersion
                    }));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri("http://localhost:5000/test"));
        request.Content = new StringContent("""{ "id": "abc" }""", Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                $$$"""
                Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: {{{expectedStatusCode}}}
                -------------------------->
                {"errors":[{"message":"The query request contains no document or no document id.","extensions":{"code":"HC0015"}}]}
                """);
    }

    [Theory]
    [InlineData(Latest)]
    public async Task Post_Should_ReturnInternalServerError_When_DocumentIsMissingBeforeCompilation(
        HttpTransportVersion transportVersion)
    {
        // arrange
        var client = GetClient(
            transportVersion,
            WellKnownRequestMiddleware.OperationResolverMiddleware,
            context => context.OperationDocumentInfo.Document = null);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });

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
                Status Code: InternalServerError
                -------------------------->
                {"errors":[{"message":"Either no query document exists or the document validation result is invalid."}]}
                """);
    }

    [Theory]
    [InlineData(Latest)]
    public async Task Post_Should_ReturnInternalServerError_When_OperationIsMissingBeforeExecution(
        HttpTransportVersion transportVersion)
    {
        // arrange
        var client = GetClient(
            transportVersion,
            WellKnownRequestMiddleware.OperationExecutionMiddleware,
            context => context.Features.GetRequired<OperationInfo>().Operation = null);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });

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
                Status Code: InternalServerError
                -------------------------->
                {"errors":[{"message":"Either no compiled operation was found or the variables have not been coerced."}]}
                """);
    }

    [Theory]
    [InlineData(Latest)]
    public async Task Post_Should_ReturnInternalServerError_When_OperationIsMissingBeforeVariableCoercion(
        HttpTransportVersion transportVersion)
    {
        // arrange
        var client = GetClient(
            transportVersion,
            WellKnownRequestMiddleware.OperationVariableCoercionMiddleware,
            context => context.Features.GetRequired<OperationInfo>().Operation = null);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });

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
                Status Code: InternalServerError
                -------------------------->
                {"errors":[{"message":"There is no operation on the context which can be used to coerce variables."}]}
                """);
    }

    [Theory]
    [InlineData("""{ "query": "{ __typename }" }""", Latest)]
    [InlineData("""{ "query": "{ __typename }", "variables": [{}] }""", Latest)]
    public async Task Post_Should_ReturnInternalServerError_When_VariablesAreMissingBeforeExecution(
        string body,
        HttpTransportVersion transportVersion)
    {
        // arrange
        var client = GetClient(
            transportVersion,
            WellKnownRequestMiddleware.OperationExecutionMiddleware,
            context => context.VariableValues = []);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

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
                Status Code: InternalServerError
                -------------------------->
                {"errors":[{"message":"Either no compiled operation was found or the variables have not been coerced."}]}
                """);
    }

    [Theory]
    [InlineData(Latest, BadRequest)]
    public async Task Post_Should_ReturnBadRequest_When_VariableBatchIsEmpty(
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode)
    {
        // arrange
        // with the cost analyzer skipped, the empty batch reaches operation execution
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .ModifyCostOptions(o => o.SkipAnalyzer = true)
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = transportVersion
                    }));
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = new StringContent(
            """{ "query": "query($id: String!) { human(id: $id) { name } }", "variables": [] }""",
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                $$$"""
                Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: {{{expectedStatusCode}}}
                -------------------------->
                {"errors":[{"message":"A variable batch request must contain at least one variable set."}]}
                """);
    }

    private HttpClient GetClient(HttpTransportVersion serverTransportVersion)
    {
        var server = CreateStarWarsServer(
            configureServices: s => s.AddGraphQLServer().AddHttpResponseFormatter(
                new HttpResponseFormatterOptions
                {
                    HttpTransportVersion = serverTransportVersion
                }));

        return server.CreateClient();
    }

    private HttpClient GetClient(
        HttpTransportVersion serverTransportVersion,
        string nextMiddleware,
        Action<Execution.RequestContext> modifyContext)
    {
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer()
                .UseRequest(
                    next => context =>
                    {
                        modifyContext(context);
                        return next(context);
                    },
                    key: "ModifyContext",
                    before: nextMiddleware)
                .AddHttpResponseFormatter(
                    new HttpResponseFormatterOptions
                    {
                        HttpTransportVersion = serverTransportVersion
                    }));

        return server.CreateClient();
    }

    private void AddAcceptHeader(HttpRequestMessage request, string? acceptHeader)
    {
        if (acceptHeader != null)
        {
            request.Headers.Add(HeaderNames.Accept, acceptHeader);
        }
    }
}

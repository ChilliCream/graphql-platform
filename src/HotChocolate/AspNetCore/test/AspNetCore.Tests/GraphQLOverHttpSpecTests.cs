using System.Net;
#if !NET11_0_OR_GREATER
using System.Net.Http.Json;
#endif
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using static System.Net.Http.HttpCompletionOption;
using static System.Net.HttpStatusCode;
using static HotChocolate.AspNetCore.HttpTransportVersion;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

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
                Status Code: NotAcceptable
                -------------------------->
                
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

    [Theory]
    [InlineData("application/json")]
    [InlineData("Application/Json")]
    [InlineData("APPLICATION/JSON")]
    [InlineData("application/json; charset=utf-8")]
    [InlineData("Application/JSON; CharSet=UTF-8")]
    public async Task Post_Should_ExecuteRequest_When_ContentTypeCasingVaries(string contentType)
    {
        // arrange
        var client = GetClient(Latest);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = new StringContent("""{"query":"{ __typename }"}""");
        request.Content.Headers.Remove("Content-Type");
        request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);

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

    [Theory]
    [InlineData("application/json-patch+json")]
    [InlineData("multipart/form-data-extended")]
    [InlineData("application/json garbage")]
    [InlineData("multipart/form-data garbage")]
    public async Task Post_Should_NotExecuteRequest_When_ContentTypeDoesNotEndAtTheMediaType(
        string contentType)
    {
        // arrange
        var client = GetClient(Latest);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = new StringContent("""{"query":"{ __typename }"}""");
        request.Content.Headers.Remove("Content-Type");
        request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Should_ReturnAllowHeader_When_OperationKindIsNotAllowed()
    {
        // arrange
        var client = GetClient(Latest);
        var query = Uri.EscapeDataString("mutation { __typename }");

        // act
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{s_url}?query={query}"));

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(MethodNotAllowed, response.StatusCode);
        Assert.Equal(["POST"], response.Content.Headers.Allow);
    }

    [Fact]
    public async Task Post_Should_NotReturnAllowHeader_When_FormatterOverridesStatusCode()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s.AddGraphQLServer()
                .AddHttpResponseFormatter<MethodNotAllowedResponseFormatter>());
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(MethodNotAllowed, response.StatusCode);
        Assert.Empty(response.Content.Headers.Allow);
    }

    [Theory]
    [InlineData("application/json;q=1.0, application/graphql-response+json;q=0.1", ContentType.Json)]
    [InlineData("application/graphql-response+json;q=0.1, application/json", ContentType.Json)]
    [InlineData("application/json;q=0.1, application/graphql-response+json;q=1.0", ContentType.GraphQLResponse)]
    [InlineData("application/graphql-response+json;q=0, application/json", ContentType.Json)]
    [InlineData("application/json, application/graphql-response+json", ContentType.GraphQLResponse)]
    public async Task SingleResult_Should_SelectHighestQualityMediaType(
        string acceptHeader,
        string expectedContentType)
    {
        // arrange
        var client = GetClient(Latest);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(OK, response.StatusCode);
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType?.ToString());
    }

    [Theory]
    [InlineData("application/graphql-response+json;q=0")]
    [InlineData("application/json;q=0, application/graphql-response+json;q=0")]
    public async Task SingleResult_Should_ReturnNotAcceptable_When_EveryMediaTypeIsRejected(
        string acceptHeader)
    {
        // arrange
        var client = GetClient(Latest);

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
                """
                Status Code: NotAcceptable
                -------------------------->
                
                """);
    }

    // The streaming media types carry incremental results only, so a single result has no
    // format to be written in when the client accepts nothing else.
    [Theory]
    [InlineData("application/graphql-response+jsonl")]
    [InlineData("application/jsonl")]
    public async Task SingleResult_Should_ReturnBareNotAcceptable_When_OnlyAStreamFormatIsAccepted(
        string acceptHeader)
    {
        // arrange
        var client = GetClient(Latest);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NotAcceptable, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentType);
        Assert.Empty(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SingleResult_Should_NotSelectMediaType_When_ASpecificRangeRejectsIt()
    {
        // arrange
        var client = GetClient(Latest);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });
        AddAcceptHeader(request, "application/graphql-response+json;q=0, */*;q=1");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.NotEqual(
            ContentType.GraphQLResponse,
            response.Content.Headers.ContentType?.ToString());
    }

    // An Accept header the server cannot parse is disregarded, and the response uses the media
    // type the configured transport serves by default. The legacy transport answers 200 there:
    // the specification scopes its 200-for-everything rule to a well-formed request, but allows
    // a 2xx for an invalid one using application/json, which is what that transport opts into.
    [Theory]
    [InlineData(Latest, BadRequest, ContentType.GraphQLResponse)]
    [InlineData(Legacy, OK, ContentType.Json)]
    public async Task Get_Should_AnswerInServerChoice_When_AcceptHeaderCannotBeParsed(
        HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode,
        string expectedContentType)
    {
        // arrange
        var client = GetClient(transportVersion);
        var url = new Uri($"{s_url}?query={Uri.EscapeDataString("{ __typename }")}");

        // act
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept", "unsupported");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType?.ToString());
    }

    // The legacy transport is pinned alongside the current one because it does not soften this
    // case: its 2xx-for-everything allowance covers responses that use application/json, and a
    // client that accepts nothing the server can write leaves no body for it to apply to.
    [Theory]
    [InlineData(Latest)]
    [InlineData(Legacy)]
    public async Task Get_Should_ReturnBareNotAcceptable_When_EveryMediaTypeIsRejected(
        HttpTransportVersion transportVersion)
    {
        // arrange
        var client = GetClient(transportVersion);
        var url = new Uri($"{s_url}?query={Uri.EscapeDataString("{ __typename }")}");

        // act
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAcceptHeader(request, "application/graphql-response+json;q=0");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NotAcceptable, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentType);
        Assert.Empty(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Head_Should_ReturnAllowHeader_When_OperationKindIsNotAllowed()
    {
        // arrange
        var client = GetClient(Latest);
        var query = Uri.EscapeDataString("mutation { __typename }");

        // act
        using var request = new HttpRequestMessage(HttpMethod.Head, new Uri($"{s_url}?query={query}"));

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(MethodNotAllowed, response.StatusCode);
        Assert.Equal(["POST"], response.Content.Headers.Allow);
    }

    // Section 12.5.1 assigns no meaning to the order of equally acceptable ranges, so the server
    // chooses: a range it treats as a request beats one it treats as a fallback, and between two
    // requests, the one the client wrote first wins. A wildcard requests the transport default,
    // which on the legacy transport is application/json, so it competes with a named GraphQL
    // media type.
    [Theory]
    [InlineData("application/graphql-response+json, application/*", ContentType.GraphQLResponse)]
    [InlineData("application/graphql-response+json, */*", ContentType.GraphQLResponse)]
    [InlineData("application/*, application/graphql-response+json", ContentType.Json)]
    [InlineData("*/*, application/graphql-response+json", ContentType.Json)]
    public async Task SingleResult_Should_SelectTheEarlierRange_When_DefaultCompetesWithNamedType(
        string acceptHeader,
        string expectedContentType)
    {
        // arrange
        var client = GetClient(Legacy);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(OK, response.StatusCode);
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType?.ToString());
    }

    // RFC 9110, section 12.5.1 resolves a media type's quality against the most specific range
    // that matches it, so a named range overrides a wildcard whether it raises the quality or
    // removes the type altogether.
    [Theory]
    [InlineData("*/*;q=0, application/*;q=1", ContentType.GraphQLResponse)]
    [InlineData("*/*;q=1, application/graphql-response+json;q=0.5", ContentType.Json)]
    [InlineData(
        "application/*;q=0, application/graphql-response+json;q=1",
        ContentType.GraphQLResponse)]
    [InlineData("text/*, application/graphql-response+json;q=0.5", ContentType.EventStream)]
    public async Task SingleResult_Should_ResolveQualityAgainstTheMostSpecificRange(
        string acceptHeader,
        string expectedContentType)
    {
        // arrange
        var client = GetClient(Latest);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(new ClientQueryRequest { Query = "{ __typename }" });
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(OK, response.StatusCode);
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType?.ToString());
    }

    [Theory]
    [InlineData("text/*", "text/event-stream; charset=utf-8")]
    [InlineData("text/*;q=0, */*;q=1", "application/graphql-response+jsonl; charset=utf-8")]
    public async Task Subscription_Should_ResolveQualityAgainstTheMostSpecificRange(
        string acceptHeader,
        string expectedContentType)
    {
        // arrange
        var client = GetClient(Latest);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(
            new ClientQueryRequest { Query = "subscription { delay(count: 1, delay: 15000) }" });
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(
            request,
            ResponseHeadersRead,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(OK, response.StatusCode);
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task DeferredResult_Should_SelectAcceptableFormat_When_WildcardOutranksRejections()
    {
        // arrange
        var client = GetClient(Latest);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(
            new ClientQueryRequest { Query = "{ ... @defer { __typename } }" });
        AddAcceptHeader(request, "multipart/mixed;q=0, text/event-stream;q=0, */*;q=1");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(OK, response.StatusCode);
        Assert.Equal(
            ContentType.GraphQLResponseStream,
            response.Content.Headers.ContentType?.ToString());
    }

    // The request flags are validated before the operation runs and cannot know which result
    // kind it will produce, so a header that is acceptable for a plain query and acceptable for
    // nothing a deferred result can be written in reaches the formatter with no usable format.
    [Fact]
    public async Task DeferredResult_Should_ExplainNotAcceptable_When_DefaultFormatIsAcceptable()
    {
        // arrange
        var client = GetClient(Latest);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(
            new ClientQueryRequest { Query = "{ ... @defer { __typename } }" });
        AddAcceptHeader(
            request,
            "multipart/mixed;q=0, text/event-stream;q=0, application/graphql-response+jsonl;q=0, "
            + "application/jsonl;q=0, */*;q=1");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NotAcceptable, response.StatusCode);
        Assert.Equal(ContentType.GraphQLResponse, response.Content.Headers.ContentType?.ToString());
        Assert.Contains(
            "HC0063",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeferredResult_Should_ReturnBareNotAcceptable_When_DefaultFormatIsRejected()
    {
        // arrange
        var client = GetClient(Latest);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, s_url);
        request.Content = JsonContent.Create(
            new ClientQueryRequest { Query = "{ ... @defer { __typename } }" });
        AddAcceptHeader(
            request,
            "application/*;q=0, multipart/mixed;q=0, text/event-stream;q=0, */*;q=1");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NotAcceptable, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentType);
        Assert.Empty(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_Should_NotAdvertiseAllow_When_PostCannotServeTheOperationKind()
    {
        // arrange
        var client = GetClient(Latest);
        var query = Uri.EscapeDataString("subscription { delay(count: 2, delay: 15000) }");

        // act
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{s_url}?query={query}"));
        AddAcceptHeader(request, ContentType.GraphQLResponse);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NotAcceptable, response.StatusCode);
        Assert.Empty(response.Content.Headers.Allow);
    }

    [Fact]
    public async Task Get_Should_ReturnNotAcceptable_When_DeferredMutationIsNotStreamable()
    {
        // arrange
        var client = GetClient(Latest);
        var query = Uri.EscapeDataString(
            """
            mutation {
                createReview(episode: NEW_HOPE, review: { stars: 5, commentary: "good" }) {
                    ... @defer { commentary }
                }
            }
            """);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{s_url}?query={query}"));
        AddAcceptHeader(request, ContentType.GraphQLResponse);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NotAcceptable, response.StatusCode);
        Assert.Empty(response.Content.Headers.Allow);
    }

    // Content suppression for HEAD is the HTTP server's responsibility and TestServer,
    // unlike Kestrel, does not emulate it, so only the status and headers are compared.
    [Fact]
    public async Task Head_Should_AnswerAsGet_When_QueryIsSupplied()
    {
        // arrange
        var client = GetClient(Latest);
        var url = new Uri($"{s_url}?query={Uri.EscapeDataString("{ __typename }")}");

        // act
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
        using var getResponse = await client.SendAsync(getRequest, TestContext.Current.CancellationToken);

        using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
        using var headResponse = await client.SendAsync(headRequest, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(OK, getResponse.StatusCode);
        Assert.Equal(getResponse.StatusCode, headResponse.StatusCode);
        Assert.Equal(
            getResponse.Content.Headers.ContentType?.ToString(),
            headResponse.Content.Headers.ContentType?.ToString());
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

    private void AddAcceptHeader(HttpRequestMessage request, string? acceptHeader)
    {
        if (acceptHeader != null)
        {
            request.Headers.Add(HeaderNames.Accept, acceptHeader);
        }
    }

    private sealed class MethodNotAllowedResponseFormatter : DefaultHttpResponseFormatter
    {
        protected override HttpStatusCode OnDetermineStatusCode(
            Execution.OperationResult result,
            FormatInfo format,
            HttpStatusCode? proposedStatusCode)
            => MethodNotAllowed;
    }
}

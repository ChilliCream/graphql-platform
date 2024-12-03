using System.Net;
using System.Net.Http.Json;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using static System.Net.Http.HttpCompletionOption;
using static System.Net.HttpStatusCode;
using static HotChocolate.AspNetCore.HttpTransportVersion;

namespace HotChocolate.AspNetCore;

public class GraphQLOverHttpSpecTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    private static readonly Uri _url = new("http://localhost:5000/graphql");

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
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __typename }", }),
        };
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request);

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
                " +
                @"{""data"":{""__typename"":""Query""}}");
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
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __typename }", }),
            Headers =
            {
                { "Accept", acceptHeader },
            },
        };

        using var response = await client.SendAsync(request);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @"Headers:
                Content-Type: multipart/mixed; boundary=""-""
                -------------------------->
                Status Code: OK
                -------------------------->

                ---
                Content-Type: application/json; charset=utf-8

                {""data"":{""__typename"":""Query""}}
                -----
                ");
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
    public async Task Query_No_Body(string? acceptHeader, HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode, string expectedContentType)
    {
        // arrange
        var client = GetClient(transportVersion);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = new ByteArrayContent([])
            {
                Headers =
                {
                    ContentType = new("application/json")
                    {
                        CharSet = "utf-8",
                    },
                },
            },
        };
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request);

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
                " +
                @"{""errors"":[{""message"":""The GraphQL request is empty."",""extensions"":{""code"":""HC0009""}}]}");
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
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __typ$ename }", }),
        };
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request);

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
                " +
                @"{""errors"":[{""message"":""Expected a `Name`-token, but found a " +
                @"`Dollar`-token."",""locations"":[{""line"":1,""column"":8}]," +
                @"""extensions"":{""code"":""HC0011""}}]}");
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
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __type name }", }),
        };
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request);

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
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __typename }", }),
        };

        request.Headers.TryAddWithoutValidation("Accept", "unsupported");

        using var response = await client.SendAsync(request);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @"Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: BadRequest
                -------------------------->
                {""errors"":[{""message"":""Unable to parse the accept header value " +
                @"`unsupported`."",""extensions"":{""headerValue"":""unsupported""," +
                @"""code"":""HC0064""}}]}");
    }

    [Fact]
    public async Task UnsupportedApplicationAcceptHeaderValue()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __typename }", }),
        };

        request.Headers.TryAddWithoutValidation("Accept", "application/unsupported");

        using var response = await client.SendAsync(request);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @"Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: NotAcceptable
                -------------------------->
                {""errors"":[{""message"":""None of the `Accept` header values is supported.""," +
                @"""extensions"":{""code"":""HC0063""}}]}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("*/*")]
    [InlineData("multipart/mixed")]
    [InlineData("multipart/*")]
    [InlineData("application/graphql-response+json, multipart/mixed")]
    [InlineData("text/event-stream, multipart/mixed")]
    public async Task DeferredQuery_Multipart(string? acceptHeader)
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ ... @defer { __typename } }", }),
        };
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @"Headers:
                Cache-Control: no-cache
                Content-Type: multipart/mixed; boundary=""-""
                -------------------------->
                Status Code: OK
                -------------------------->

                ---
                Content-Type: application/json; charset=utf-8

                {""data"":{},""hasNext"":true}
                ---
                Content-Type: application/json; charset=utf-8

                {""incremental"":[{""data"":{""__typename"":""Query""}," +
                @"""path"":[]}],""hasNext"":false}
                -----
                ");
    }

    [Theory]
    [InlineData("text/event-stream")]
    [InlineData("application/graphql-response+json, text/event-stream")]
    public async Task DeferredQuery_EventStream(string acceptHeader)
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ ... @defer { __typename } }", }),
            Headers = { { "Accept", acceptHeader }, },
        };

        using var response = await client.SendAsync(request, ResponseHeadersRead);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @"Headers:
                Cache-Control: no-cache
                Content-Type: text/event-stream; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                event: next
                data: {""data"":{},""hasNext"":true}

                event: next
                data: {""incremental"":[{""data"":{""__typename"":""Query""}," +
                @"""path"":[]}],""hasNext"":false}

                event: complete

                ");
    }

    [Fact]
    public async Task DeferredQuery_NoStreamableAcceptHeader()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ ... @defer { __typename } }", }),
            Headers = { { "Accept", ContentType.GraphQLResponse }, },
        };

        using var response = await client.SendAsync(request, ResponseHeadersRead);

        // assert
        // we are rejecting the request since we have a streamed result and
        // the user requests a json payload.
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @"Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: MethodNotAllowed
                -------------------------->
                {""errors"":[{""message"":""The specified operation kind is not allowed.""}]}");
    }

    [Fact]
    public async Task EventStream_Sends_KeepAlive()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url);
        request.Content = JsonContent.Create(
            new ClientQueryRequest
            {
                Query = "subscription {delay(count: 2, delay:15000)}",
            });
        request.Headers.Add("Accept", "text/event-stream");

        using var response = await client.SendAsync(request, ResponseHeadersRead);

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
        using var request = new HttpRequestMessage(HttpMethod.Post, _url);
        request.Content = JsonContent.Create(
            new ClientQueryRequest
            {
                Query = "subscription {delay(count: 2, delay:15000)}",
            });
        request.Headers.Add("Accept", "*/*");

        using var response = await client.SendAsync(request, ResponseHeadersRead);

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
        using var request = new HttpRequestMessage(HttpMethod.Post, _url);
        request.Content = JsonContent.Create(
            new ClientQueryRequest
            {
                Query = "subscription foo @foo(bar: 1) {delay(count: 2, delay:15000)}",
            });
        request.Headers.Add("Accept", "*/*");

        using var response = await client.SendAsync(request, ResponseHeadersRead);

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
                    """),
            ]),
            new Uri("http://localhost:5000/graphql"));

        using var response = await client.SendAsync(request);

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

        await snapshot.MatchMarkdownAsync();
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
                    new Dictionary<string, object?> { { "episode", "NEW_HOPE" }, },
                    new Dictionary<string, object?> { { "episode", "EMPIRE" }, },
                ]),
            new Uri("http://localhost:5000/graphql"));

        using var response = await client.SendAsync(request);

        // assert
        Assert.Equal(OK, response.StatusCode);

        await foreach (var result in response.ReadAsResultStreamAsync())
        {
            snapshot.Add(result);
        }

        await snapshot.MatchMarkdownAsync();
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

        using var response = await client.SendAsync(request);

        // assert
        Assert.Equal(OK, response.StatusCode);
    }

    private HttpClient GetClient(HttpTransportVersion serverTransportVersion)
    {
        var server = CreateStarWarsServer(
            configureServices: s => s.AddHttpResponseFormatter(
                new HttpResponseFormatterOptions
                {
                    HttpTransportVersion = serverTransportVersion,
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

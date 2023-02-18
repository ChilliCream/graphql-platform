using System.Net;
using System.Net.Http.Json;
using CookieCrumble;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using static System.Net.Http.HttpCompletionOption;
using static System.Net.HttpStatusCode;
using static HotChocolate.AspNetCore.HttpTransportVersion;

namespace HotChocolate.AspNetCore;

// todo: test multiple accept header values
// todo: test invalid accept header values
// todo: test bubbling error resulting in http 500
public class GraphQLOverHttpSpecTests : ServerTestBase
{
    private static readonly Uri _url = new("http://localhost:5000/graphql");

    public GraphQLOverHttpSpecTests(TestServerFactory serverFactory)
        : base(serverFactory) { }

    [Theory]
    [InlineData(null, Latest, OK, ContentType.GraphQLResponse)]
    [InlineData(null, Legacy, OK, ContentType.Json)]
    [InlineData("*/*", Latest, OK, ContentType.GraphQLResponse)]
    [InlineData("*/*", Legacy, OK, ContentType.Json)]
    [InlineData("application/*", Latest, OK, ContentType.GraphQLResponse)]
    [InlineData("application/*", Legacy, OK, ContentType.Json)]
    [InlineData(ContentType.Json, Latest, OK, ContentType.Json)]
    [InlineData(ContentType.Json, Legacy, OK, ContentType.Json)]
    [InlineData(ContentType.GraphQLResponse, Latest, OK, ContentType.GraphQLResponse)]
    [InlineData(ContentType.GraphQLResponse, Legacy, OK, ContentType.GraphQLResponse)]
    public async Task SingleResult_Success(string? acceptHeader, HttpTransportVersion transportVersion,
        HttpStatusCode expectedStatusCode, string expectedContentType)
    {
        // arrange
        var client = GetClient(transportVersion);

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __typename }" })
        };
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @$"Headers:
                Content-Type: {expectedContentType.Replace(";", "; ")}
                -------------------------->
                Status Code: {expectedStatusCode}
                -------------------------->
                " +
                @"{""data"":{""__typename"":""Query""}}");
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
            Content = new ByteArrayContent(Array.Empty<byte>())
            {
                Headers = { ContentType = new("application/json") { CharSet = "utf-8" } }
            }
        };
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @$"Headers:
                Content-Type: {expectedContentType.Replace(";", "; ")}
                -------------------------->
                Status Code: {expectedStatusCode}
                -------------------------->
                " +
                @"{""errors"":[{""message"":""The GraphQL request is empty."",""extensions"":{""code"":""HC0012""}}]}");
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
                new ClientQueryRequest { Query = "{ __typ$ename }" })
        };
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @$"Headers:
                Content-Type: {expectedContentType.Replace(";", "; ")}
                -------------------------->
                Status Code: {expectedStatusCode}
                -------------------------->
                " +
                @"{""errors"":[{""message"":""Expected a \u0060Name\u0060-token, but found a " +
                @"\u0060Dollar\u0060-token."",""locations"":[{""line"":1,""column"":8}]," +
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
                new ClientQueryRequest { Query = "{ __type name }" })
        };
        AddAcceptHeader(request, acceptHeader);

        using var response = await client.SendAsync(request);

        // assert
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @$"Headers:
                Content-Type: {expectedContentType.Replace(";", "; ")}
                -------------------------->
                Status Code: {expectedStatusCode}
                -------------------------->
                " +
                @"{""errors"":[{""message"":""\u0060__type\u0060 is an object, interface or " +
                @"union type field. Leaf selections on objects, interfaces, and unions without " +
                @"subfields are disallowed."",""locations"":[{""line"":1,""column"":3}]," +
                @"""extensions"":{""declaringType"":""Query"",""field"":""__type""," +
                @"""type"":""__Type"",""responseName"":""__type""," +
                @"""specifiedBy"":""http://spec.graphql.org/October2021/#sec-Field-Selections-" +
                @"on-Objects-Interfaces-and-Unions-Types""}},{""message"":""The field \u0060name" +
                @"\u0060 does not exist on the type \u0060Query\u0060."",""locations"":[{" +
                @"""line"":1,""column"":10}],""extensions"":{""type"":""Query""," +
                @"""field"":""name"",""responseName"":""name"",""specifiedBy"":" +
                @"""http://spec.graphql.org/October2021/#sec-Field-Selections-on-Objects-" +
                @"Interfaces-and-Unions-Types""}},{""message"":""The argument \u0060name\u0060 " +
                @"is required."",""locations"":[{""line"":1,""column"":3}],""extensions"":{" +
                @"""type"":""Query"",""field"":""__type"",""argument"":""name""," +
                @"""specifiedBy"":""http://spec.graphql.org/October2021/#sec-Required-Arguments""" +
                "}}]}");
    }

    private HttpClient GetClient(HttpTransportVersion serverTransportVersion)
    {
        var server = CreateStarWarsServer(
               configureServices: s => s.AddHttpResponseFormatter(
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

    /// <summary>
    /// This request does not specify a accept header.
    /// expected response content-type: multipart/mixed
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task Legacy_With_Stream_1()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ ... @defer { __typename } }" })
        };

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: multipart/mixed
        // expected status code: 200
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

    /// <summary>
    /// This request specifies the multipart/mixed accept header.
    /// expected response content-type: multipart/mixed
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task New_Query_No_Streams_3()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __typename }" }),
            Headers =
            {
                { "Accept", $"{ContentType.Types.MultiPart}/{ContentType.SubTypes.Mixed}" }
            }
        };

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: multipart/mixed
        // expected status code: 200
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

    /// <summary>
    /// This request specifies the application/graphql-response+json and
    /// the multipart/mixed content type as accept header value.
    /// expected response content-type: application/graphql-response+json
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task New_Query_No_Streams_4()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __typename }" }),
            Headers =
            {
                {
                    "Accept",
                    new[]
                    {
                        ContentType.GraphQLResponse,
                        $"{ContentType.Types.MultiPart}/{ContentType.SubTypes.Mixed}"
                    }
                }
            }
        };

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: application/graphql-response+json
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
                {""data"":{""__typename"":""Query""}}");
    }

    /// <summary>
    /// This request specifies the text/event-stream, multipart/mixed, application/json and
    /// application/graphql-response+json content types as accept header value.
    /// expected response content-type: application/graphql-response+json
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task New_Query_No_Streams_9()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __typename }" }),
            Headers =
            {
                {
                    "Accept",
                    new[]
                    {
                        ContentType.EventStream,
                        $"{ContentType.Types.MultiPart}/{ContentType.SubTypes.Mixed}",
                        ContentType.Json,
                        ContentType.GraphQLResponse,
                    }
                }
            }
        };

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: application/graphql-response+json
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
                {""data"":{""__typename"":""Query""}}");
    }

    /// <summary>
    /// This request specifies the application/unsupported content types as accept header value.
    /// expected response content-type: application/graphql-response+json
    /// expected status code: 400
    /// </summary>
    [Fact]
    public async Task New_Query_No_Streams_10()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __typename }" })
        };

        request.Headers.TryAddWithoutValidation("Accept", "unsupported");

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: application/graphql-response+json
        // expected status code: 400
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
                @"\u0060unsupported\u0060."",""extensions"":{""headerValue"":""unsupported""," +
                @"""code"":""HC0064""}}]}");
    }

    /// <summary>
    /// This request specifies the application/unsupported content types as accept header value.
    /// expected response content-type: application/graphql-response+json
    /// expected status code: 206
    /// </summary>
    [Fact]
    public async Task New_Query_No_Streams_12()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __typename }" })
        };

        request.Headers.TryAddWithoutValidation("Accept", "application/unsupported");

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: application/graphql-response+json
        // expected status code: 206
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @"Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: NotAcceptable
                -------------------------->
                {""errors"":[{""message"":""None of the proved accept header media types " +
                @"is supported."",""extensions"":{""code"":""HC0063""}}]}");
    }

    /// <summary>
    /// This request specifies the application/graphql-response+json; charset=utf-8,
    /// multipart/mixed; charset=utf-8 content types as accept header value.
    /// expected response content-type: application/graphql-response+json
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task New_Query_No_Streams_13()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ __typename }" })
        };

        request.Headers.TryAddWithoutValidation(
            "Accept",
            "application/graphql-response+json; charset=utf-8, multipart/mixed; charset=utf-8");

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: application/graphql-response+json
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
                {""data"":{""__typename"":""Query""}}");
    }

    /// <summary>
    /// This request specifies the application/graphql-response+json and
    /// the multipart/mixed content type as accept header value.
    /// expected response content-type: multipart/mixed
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task New_Query_With_Streams_1()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ ... @defer { __typename } }" }),
            Headers =
            {
                {
                    "Accept",
                    new[]
                    {
                        ContentType.GraphQLResponse,
                        $"{ContentType.Types.MultiPart}/{ContentType.SubTypes.Mixed}"
                    }
                }
            }
        };

        using var response = await client.SendAsync(request, ResponseHeadersRead);

        // assert
        // expected response content-type: multipart/mixed
        // expected status code: 200
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

    /// <summary>
    /// This request specifies the application/graphql-response+json
    /// content type as accept header value.
    /// expected response content-type: application/graphql-response+json
    /// expected status code: 405
    /// </summary>
    [Fact]
    public async Task New_Query_With_Streams_2()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ ... @defer { __typename } }" }),
            Headers = { { "Accept", new[] { ContentType.GraphQLResponse } } }
        };

        using var response = await client.SendAsync(request, ResponseHeadersRead);

        // assert
        // expected response content-type: application/graphql-response+json
        // expected status code: 405
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

    /// <summary>
    /// This request specifies the text/event-stream content type as accept header value.
    /// expected response content-type: text/event-stream
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task New_Query_With_Streams_3()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest { Query = "{ ... @defer { __typename } }" }),
            Headers = { { "Accept", new[] { ContentType.EventStream } } }
        };

        using var response = await client.SendAsync(request, ResponseHeadersRead);

        // assert
        // expected response content-type: text/event-stream
        // expected status code: 200
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
}

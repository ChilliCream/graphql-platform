#if NET6_0_OR_GREATER
using System.Net.Http.Json;
using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;
using static System.Net.Http.HttpCompletionOption;

namespace HotChocolate.AspNetCore;

public class GraphQLOverHttpSpecTests : ServerTestBase
{
    private static readonly Uri _url = new("http://localhost:5000/graphql");

    public GraphQLOverHttpSpecTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    /// <summary>
    /// This request does not specify a accept header.
    /// expected response content-type: application/json
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task Legacy_Query_No_Streams_1()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest
                {
                    Query = "{ __typename }"
                })
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
                Content-Type: application/json; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                {""data"":{""__typename"":""Query""}}");
    }

    /// <summary>
    /// This request does not specify a accept header and has a syntax error.
    /// expected response content-type: application/json
    /// expected status code: 400
    /// </summary>
    [Fact]
    public async Task Legacy_Query_No_Streams_2()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest
                {
                    Query = "{ __typ$ename }"
                })
        };

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: application/json
        // expected status code: 400
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @"Headers:
                Content-Type: application/json; charset=utf-8
                -------------------------->
                Status Code: BadRequest
                -------------------------->
                {""errors"":[{""message"":""Expected a \u0060Name\u0060-token, but found a " +
                @"\u0060Dollar\u0060-token."",""locations"":[{""line"":1,""column"":8}]," +
                @"""extensions"":{""code"":""HC0011""}}]}");
    }

    /// <summary>
    /// This request does not specify a accept header.
    /// expected response content-type: application/json
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task Legacy_Query_No_Streams_3()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest
                {
                    Query = "{ __type name }"
                })
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
                Content-Type: application/json; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                {""data"":{""__typename"":""Query""}}");
    }

    /// <summary>
    /// This request specifies the application/graphql-response+json accept header.
    /// expected response content-type: application/graphql-response+json
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task New_Query_No_Streams_1()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest
                {
                    Query = "{ __typename }"
                }),
            Headers =
            {
                { "Accept", ContentType.GraphQLResponse }
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
    /// This request specifies the application/json accept header.
    /// expected response content-type: application/json
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task New_Query_No_Streams_2()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest
                {
                    Query = "{ __typename }"
                }),
            Headers =
            {
                { "Accept", ContentType.Json }
            }
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
                Content-Type: application/json; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                {""data"":{""__typename"":""Query""}}");
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
                new ClientQueryRequest
                {
                    Query = "{ __typename }"
                }),
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
                Content-Type: multipart/mixed; boundary=""-""; charset=utf-8
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
                new ClientQueryRequest
                {
                    Query = "{ __typename }"
                }),
            Headers =
            {
                { "Accept", new[]
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
    /// This request specifies the */j* accept header.
    /// expected response content-type: application/graphql-response+json; charset=utf-8
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task New_Query_No_Streams_5()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest
                {
                    Query = "{ __typename }"
                }),
            Headers =
            {
                { "Accept", "*/*" }
            }
        };

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: application/graphql-response+json; charset=utf-8
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
    /// This request specifies the */j* accept header.
    /// expected response content-type: application/graphql-response+json; charset=utf-8
    /// expected status code: 200
    /// </summary>
    [Fact]
    public async Task New_Query_No_Streams_6()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(
                new ClientQueryRequest
                {
                    Query = "{ __typename }"
                }),
            Headers =
            {
                { "Accept", "application/*" }
            }
        };

        using var response = await client.SendAsync(request);

        // assert
        // expected response content-type: application/graphql-response+json; charset=utf-8
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
                new ClientQueryRequest
                {
                    Query = "{ ... @defer { __typename } }"
                }),
            Headers =
            {
                { "Accept", new[]
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
                Content-Type: multipart/mixed; boundary=""-""; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->

                ---
                Content-Type: application/json; charset=utf-8

                {""data"":{},""hasNext"":true}
                ---
                Content-Type: application/json; charset=utf-8

                {""path"":[],""data"":{""__typename"":""Query""},""hasNext"":false}
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
                new ClientQueryRequest
                {
                    Query = "{ ... @defer { __typename } }"
                }),
            Headers =
            {
                { "Accept", new[] { ContentType.GraphQLResponse } }
            }
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


}
#endif

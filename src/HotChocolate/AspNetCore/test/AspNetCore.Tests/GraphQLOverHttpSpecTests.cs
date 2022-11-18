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
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: OK
                -------------------------->
                {""data"":{""__typename"":""Query""}}");
    }

     /// <summary>
        /// This request does not specify a accept header.
        /// expected response content-type: application/json
        /// expected status code: 200
        /// </summary>
        [Fact]
        public async Task Query_No_Body()
        {
            // arrange
            var server = CreateStarWarsServer();
            var client = server.CreateClient();

            // act
            using var request = new HttpRequestMessage(HttpMethod.Post, _url)
            {
                Content = new ByteArrayContent(Array.Empty<byte>())
                {
                    Headers = { ContentType = new("application/json") { CharSet = "utf-8" } }
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
                    Content-Type: application/graphql-response+json; charset=utf-8
                    -------------------------->
                    Status Code: BadRequest
                    -------------------------->
                    {""errors"":[{""message"":""The GraphQL request is empty."",""extensions"":{""code"":""HC0012""}}]}");
        }

    /// <summary>
    /// This request does not specify a accept header and has a syntax error.
    /// expected response content-type: application/json
    /// expected status code: 200
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
        // expected status code: 200
        Snapshot
            .Create()
            .Add(response)
            .MatchInline(
                @"Headers:
                Content-Type: application/graphql-response+json; charset=utf-8
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
                Content-Type: application/graphql-response+json; charset=utf-8
                -------------------------->
                Status Code: BadRequest
                -------------------------->
                {""errors"":[{""message"":""\u0060__type\u0060 is an object, interface or " +
                "union type field. Leaf selections on objects, interfaces, and unions without " +
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
                new ClientQueryRequest
                {
                    Query = "{ ... @defer { __typename } }"
                })
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
    /// This request specifies the */* accept header.
    /// expected response content-type: application/json; charset=utf-8
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
        // expected response content-type: application/json; charset=utf-8
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
    /// This request specifies the application/* accept header.
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
    /// This request does not specify a application/graphql-response+json accept header and
    /// has a syntax error.
    /// expected response content-type: application/graphql-response+json
    /// expected status code: 400
    /// </summary>
    [Fact]
    public async Task New_Query_No_Streams_7()
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
                }),
            Headers =
            {
                { "Accept", ContentType.GraphQLResponse }
            }
        };

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
                {""errors"":[{""message"":""Expected a \u0060Name\u0060-token, but found a " +
                @"\u0060Dollar\u0060-token."",""locations"":[{""line"":1,""column"":8}]," +
                @"""extensions"":{""code"":""HC0011""}}]}");
    }

    /// <summary>
    /// This request does not specify a application/graphql-response+json accept header and
    /// has a syntax error.
    /// expected response content-type: application/graphql-response+json
    /// expected status code: 400
    /// </summary>
    [Fact]
    public async Task New_Query_No_Streams_8()
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
                }),
            Headers =
            {
                { "Accept", ContentType.GraphQLResponse }
            }
        };

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
                {""errors"":[{""message"":""\u0060__type\u0060 is an object, interface or " +
                "union type field. Leaf selections on objects, interfaces, and unions without " +
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
                new ClientQueryRequest
                {
                    Query = "{ __typename }"
                }),
            Headers =
            {
                { "Accept", new[]
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
                new ClientQueryRequest
                {
                    Query = "{ __typename }"
                })
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
                new ClientQueryRequest
                {
                    Query = "{ __typename }"
                })
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
                new ClientQueryRequest
                {
                    Query = "{ __typename }"
                })
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
                new ClientQueryRequest
                {
                    Query = "{ ... @defer { __typename } }"
                }),
            Headers =
            {
                { "Accept", new[]
                    {
                        ContentType.EventStream
                    }
                }
            }
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
#endif

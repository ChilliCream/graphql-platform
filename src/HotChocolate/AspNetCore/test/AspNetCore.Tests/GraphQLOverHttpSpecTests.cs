#if NET6_0_OR_GREATER
using System.Net;
using System.Net.Http.Json;
using CookieCrumble;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore;

public class GraphQLOverHttpSpecTests : ServerTestBase
{
    private static readonly Uri _url = new("http://localhost:5000/graphql");

    public GraphQLOverHttpSpecTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task No_AcceptHeader_Specified_SingleResult_ApplicationsJson()
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
        response.MatchSnapshot();
    }

    [Fact]
    public async Task GraphQL_AcceptHeader_Specified_SingleResult_GraphQLResponse()
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
        response.MatchSnapshot();
    }

    [Fact]
    public async Task MultiPart_AcceptHeader_Specified_SingleResult_MultiPartResponse()
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
                { "Accept", ContentType.MultiPartMixed }
            }
        };

        using var response = await client.SendAsync(request);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task MultiPart_GraphQL_AcceptHeader_Specified_SingleResult_MultiPartResponse()
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
                { "Accept", new[] { ContentType.MultiPartMixed, ContentType.GraphQLResponse } }
            }
        };

        using var response = await client.SendAsync(request);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task GraphQL_MultiPart_AcceptHeader_Specified_SingleResult_GraphQLResponse()
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
                { "Accept", new[] { ContentType.GraphQLResponse, ContentType.MultiPartMixed } }
            }
        };

        using var response = await client.SendAsync(request);

        // assert
        response.MatchSnapshot();
    }
}
#endif

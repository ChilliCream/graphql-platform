using System.Net;
using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Builder;

namespace HotChocolate.AspNetCore;

public class HttpGetSchemaMiddlewareTests : ServerTestBase
{
    public HttpGetSchemaMiddlewareTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task Download_GraphQL_SDL()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_Types_SDL()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl&types=Query");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_Types_SDL_Character_and_Query()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl&types=Character,Query");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_Types_SDL_Type_Not_Found()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl&types=Xyz");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_Types_SDL_Types_Empty()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl&types=");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_Types_SDL_Invalid_TypeName()
    {
        // arrange
        var server = CreateStarWarsServer();
        var url = TestServerExtensions.CreateUrl("/graphql?sdl&types=Xyz.Abc");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_SDL_Explicit_Route()
    {
        // arrange
        var server = CreateServer(b => b.MapGraphQLSchema());
        var url = TestServerExtensions.CreateUrl("/graphql/sdl");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_SDL_Explicit_Route_Explicit_Pattern()
    {
        // arrange
        var server = CreateServer(b => b.MapGraphQLSchema("/foo/bar"));
        var url = TestServerExtensions.CreateUrl("/foo/bar");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Download_GraphQL_SDL_Disabled()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureConventions: e => e.WithOptions(
                new GraphQLServerOptions
                {
                    EnableSchemaRequests = false,
                    Tool = { Enable = false, },
                }));
        var url = TestServerExtensions.CreateUrl("/graphql?sdl");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // act
        var response = await server.CreateClient().SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        result.MatchSnapshot();
    }
}

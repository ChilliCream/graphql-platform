#if NET8_0_OR_GREATER
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.AspNetCore;

public class PersistedOperationMiddlewareTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task ExecutePersistedOperation_Success()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var result = await client.GetAsync("/graphql/persisted/60ddx_GGk4FDObSa6eK0sg/GetHeroName");

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
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
        var result = await client.GetAsync("/graphql/persisted/60ddx_GGk4FDObSa6eK0s1/GetHeroName");

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
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
        var result = await client.GetAsync("/graphql/persisted/60ddx_GG+k4FDObSa6eK0s1/GetHeroName");

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
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
            body);

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
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
            body);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
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
            body);

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
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
            body);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
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
            body);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
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
            body);

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
        json!.RootElement.MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData(null)]
    public async Task Post_Should_ReturnNotFound_When_ContentTypeIsNotJson(string? contentType)
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();

        // act
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "http://localhost:5000/graphql/persisted/60ddx_GGk4FDObSa6eK0sg/GetHeroName");
        request.Content = contentType is null
            ? new ByteArrayContent(Encoding.UTF8.GetBytes("{ }"))
            : new StringContent("{ }", Encoding.UTF8, contentType);

        using var response = await client.SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
    }
}
#endif

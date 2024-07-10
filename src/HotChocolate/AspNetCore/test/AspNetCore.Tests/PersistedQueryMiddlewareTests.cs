#if NET8_0_OR_GREATER
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.AspNetCore;

public class PersistedQueryMiddlewareTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task ExecutePersistedQuery_Success()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var result = await client.GetAsync("/graphql/q/60ddx_GGk4FDObSa6eK0sg/Test");

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
        json!.RootElement.MatchMarkdownSnapshot();
    }
    
    [Fact]
    public async Task ExecutePersistedQuery_NotFound()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var result = await client.GetAsync("/graphql/q/60ddx_GGk4FDObSa6eK0s1/Test");

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
        json!.RootElement.MatchMarkdownSnapshot();
    }
    
    [Fact]
    public async Task ExecutePersistedQuery_InvalidId()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000");

        // act
        var result = await client.GetAsync("/graphql/q/60ddx_GG+k4FDObSa6eK0s1/Test");

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
        json!.RootElement.MatchMarkdownSnapshot();
    }
    
    [Fact]
    public async Task ExecutePersistedQuery_HttpPost_Empty_Body_Success()
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
            "/graphql/q/60ddx_GGk4FDObSa6eK0sg/Test",
            body);

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
        json!.RootElement.MatchMarkdownSnapshot();
    }
    
    [Fact]
    public async Task ExecutePersistedQuery_HttpPost_Empty_Body_NotFound()
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
            "/graphql/q/60ddx_GGk4FDObSa6eK0sg1/Test",
            body);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
        json!.RootElement.MatchMarkdownSnapshot();
    }
    
    [Fact]
    public async Task ExecutePersistedQuery_HttpPost_Empty_Body_InvalidId()
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
            "/graphql/q/60ddx_GGk4+FDObSa6eK0sg1/Test",
            body);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
        json!.RootElement.MatchMarkdownSnapshot();
    }
    
    [Fact]
    public async Task ExecutePersistedQuery_HttpPost_With_Variables_Success()
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
            "/graphql/q/abc123/Test",
            body);

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadFromJsonAsync<JsonDocument>();
        json!.RootElement.MatchMarkdownSnapshot();
    }
}
#endif
#if NET8_0_OR_GREATER
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.AspNetCore;

public class PersistedQueryMiddlewareTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task Simple_IsAlive_Test()
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

}
#endif
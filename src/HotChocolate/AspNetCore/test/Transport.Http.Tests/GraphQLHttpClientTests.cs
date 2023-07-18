using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.Transport.Http.Tests;

public class GraphQLHttpClientTests : ServerTestBase
{
    /// <inheritdoc />
    public GraphQLHttpClientTests(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task ExecutePostAsync_Returns_OperationResult()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(TestServerExtensions.CreateUrl("/graphql"));
        var client = new DefaultGraphQLHttpClient(httpClient);
        var request = new OperationRequest("query { hero(episode: JEDI) { name } }");
            
        // act
        var response = await client.ExecuteAsync(request, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteGetAsync_Returns_OperationResult()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(TestServerExtensions.CreateUrl("/graphql"));
        var client = new DefaultGraphQLHttpClient(httpClient);
        var request = new GraphQLHttpRequest(
            new OperationRequest("query { hero(episode: JEDI) { name } }"))
        {
            Method = GraphQLHttpMethod.Get
        };
            
        // act
        var response = await client.ExecuteAsync(request, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteGetAsync_WithVariablesReturns_OperationResult()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(TestServerExtensions.CreateUrl("/graphql"));
        var client = new DefaultGraphQLHttpClient(httpClient);
        var request = new GraphQLHttpRequest(
            new OperationRequest(
                "query { hero(episode: JEDI) { name } }",
                variables: new Dictionary<string, object?>()
                {
                    {"episode", "JEDI"}
                }))
        {
            Method = GraphQLHttpMethod.Get
        };
            
        // act
        var response = await client.ExecuteAsync(request, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchSnapshot();
    }
}

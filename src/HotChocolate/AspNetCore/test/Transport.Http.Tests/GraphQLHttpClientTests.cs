using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;
using static HotChocolate.AspNetCore.Tests.Utilities.TestServerExtensions;

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
        var client = new DefaultGraphQLHttpClient(httpClient);
        var request = new GraphQLHttpRequest("query { hero(episode: JEDI) { name } }", new Uri(CreateUrl("/graphql")));

        // act
        using var response = await client.SendAsync(request, cts.Token);

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
        var client = new DefaultGraphQLHttpClient(httpClient);
        var request = new GraphQLHttpRequest(
            new OperationRequest("query { hero(episode: JEDI) { name } }"),
            new Uri(CreateUrl("/graphql")))
        {
            Method = GraphQLHttpMethod.Get
        };

        // act
        var response = await client.SendAsync(request, cts.Token);

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
        var client = new DefaultGraphQLHttpClient(httpClient);
        var request = new GraphQLHttpRequest(
            new OperationRequest(
                "query($episode: Episode!) { hero(episode: $episode) { name } }",
                variables: new Dictionary<string, object?>()
                {
                    {"episode", "JEDI"}
                }), new Uri(CreateUrl("/graphql")))
        {
            Method = GraphQLHttpMethod.Get
        };

        // act
        var response = await client.SendAsync(request, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchSnapshot();
    }

    [Fact(Skip = "Needs to be implemented.")]
    public async Task Execute_Subscription_Over_SSE()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));
        var client = new DefaultGraphQLHttpClient(httpClient);
        var request = new GraphQLHttpRequest(
            new OperationRequest("subscription { onReview(episode: JEDI) { stars } }"));

        // act
        var response = await client.SendAsync(request, cts.Token);

        // assert
        await foreach (var result in response.ReadAsResultStreamAsync(cts.Token).WithCancellation(cts.Token))
        {
            result.MatchSnapshot();
            cts.Cancel();
        }
    }
}

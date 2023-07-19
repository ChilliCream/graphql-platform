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
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));
        var client = new DefaultGraphQLHttpClient(httpClient);
        var request = new GraphQLHttpRequest("query { hero(episode: JEDI) { name } }");
            
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
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));
        var client = new DefaultGraphQLHttpClient(httpClient);
        var request = new GraphQLHttpRequest(
            new OperationRequest("query { hero(episode: JEDI) { name } }"))
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
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));
        var client = new DefaultGraphQLHttpClient(httpClient);
        var request = new GraphQLHttpRequest(
            new OperationRequest(
                "query($episode: Episode!) { hero(episode: $episode) { name } }",
                variables: new Dictionary<string, object?>()
                {
                    {"episode", "JEDI"}
                }))
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
    public async Task Execute_Subscription_Over_SSE()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(50000));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));

        var subscriptionRequest = new GraphQLHttpRequest(
            """
            subscription { 
              onReview(episode: JEDI) { 
                stars 
              } 
            }
            """);
        
        var mutationRequest = new GraphQLHttpRequest(
            new OperationRequest(
                """
                mutation CreateReviewForEpisode(
                    $ep: Episode!, $review: ReviewInput!) {
                    createReview(episode: $ep, review: $review) {
                        stars
                        commentary
                    }
                }
                """,
                variables: new Dictionary<string, object?>
                {
                    ["ep"] = "JEDI",
                    ["review"] = new Dictionary<string, object?>
                    {
                        ["stars"] = 5,
                        ["commentary"] = "This is a great movie!"
                    }
                }));
        
        var client = new DefaultGraphQLHttpClient(httpClient);
            
        // act
        var subscriptionResponse = await client.SendAsync(subscriptionRequest, cts.Token);
        var mutationResponse = await client.SendAsync(mutationRequest, cts.Token);
        
        mutationResponse.EnsureSuccessStatusCode();

        // assert
        await foreach (var result in subscriptionResponse.ReadAsResultStreamAsync(cts.Token))
        {
            result.MatchSnapshot();    
            cts.Cancel();
        }
    }
}

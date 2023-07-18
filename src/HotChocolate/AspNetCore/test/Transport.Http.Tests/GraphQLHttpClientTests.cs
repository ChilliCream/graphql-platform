using HotChocolate.AspNetCore.Tests.Utilities;
using Snapshooter.Xunit;

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
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(TestServerExtensions.CreateUrl("/graphql"));
        var graphQLHttpClient = new GraphQLHttpClient(httpClient);

        // act
        var response = await graphQLHttpClient.ExecutePostAsync(
            new OperationRequest("query { hero(episode: JEDI) { name } }"),
            new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

        // assert
        response.Data.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteGetAsync_Returns_OperationResult()
    {
        // arrange
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(TestServerExtensions.CreateUrl("/graphql"));
        var graphQLHttpClient = new GraphQLHttpClient(httpClient);

        // act
        var response = await graphQLHttpClient.ExecuteGetAsync(
            new OperationRequest("query { hero(episode: JEDI) { name } }"),
            new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

        // assert
        response.Data.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteGetAsync_WithVariablesReturns_OperationResult()
    {
        // arrange
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(TestServerExtensions.CreateUrl("/graphql"));
        var graphQLHttpClient = new GraphQLHttpClient(httpClient);

        // act
        var response = await graphQLHttpClient.ExecuteGetAsync(
            new OperationRequest(
                "query($episode: Episode) { hero(episode: $episode) { name } }",
                variables: new Dictionary<string, object?>()
                {
                    {"episode", "JEDI"}
                }),
            new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

        // assert
        response.Data.ToString().MatchSnapshot();
    }
}

using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Schemas.Reviews;
using HotChocolate.Fusion.Schemas.Accounts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class DemoIntegrationTests
{
    private readonly TestServerFactory _testServerFactory = new();

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserReviews()
    {
        // arrange
        using var reviews = _testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<ReviewRepository>()
                .AddGraphQLServer()
                .AddQueryType<ReviewQuery>(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        using var accounts = _testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<UserRepository>()
                .AddGraphQLServer()
                .AddQueryType<AccountQuery>(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        var clients = new Dictionary<string, Func<HttpClient>>
        {
            {
                "Reviews",
                () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = reviews.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    return httpClient;
                }
            },
            {
                "Accounts",
                () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = accounts.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    return httpClient;
                }
            },
        };

        var clientFactory = new RemoteQueryExecutorTests.MockHttpClientFactory(clients);

        var request = Parse(
            """
            query {
                users {
                    name
                    reviews {
                        body
                        author {
                            name
                        }
                    }
                }
            }
            """);

         var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(clientFactory)
            .AddFusionGatewayServer(FileResource.Open("AccountsAndReviews.graphql"))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();

        snapshot.Add(request, "User Request");

        if (result.ContextData is not null &&
            result.ContextData.TryGetValue("queryPlan", out var value) &&
            value is QueryPlan queryPlan)
        {
            snapshot.Add(queryPlan, "QueryPlan");
        }

        snapshot.Add(result, "Result");

        await snapshot.MatchAsync();

    }
}

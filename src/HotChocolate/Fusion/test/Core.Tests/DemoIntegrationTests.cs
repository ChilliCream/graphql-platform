using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Shared;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class DemoIntegrationTests
{
    [Fact]
    public async Task Authors_And_Reviews_AutoCompose()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        // assert
        SchemaFormatter
            .FormatAsString(fusionGraph)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_AutoCompose()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        // assert
        SchemaFormatter
            .FormatAsString(fusionGraph)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserReviews()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsString(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query GetUser {
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

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserById()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsString(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query GetUser {
              userById(id: 1) {
                id
              }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Authors_And_Reviews_Subscription_OnNewReview()
    {
        // arrange
        using var cts = new CancellationTokenSource(10_000);
        using var demoProject = await DemoProject.CreateAsync(cts.Token);

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            },
            cts.Token);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsString(fusionGraph))
            .BuildRequestExecutorAsync(cancellationToken: cts.Token);

        var request = Parse(
            """
            subscription OnNewReview {
                onNewReview {
                    body
                    author {
                        name
                    }
                }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create(),
            cts.Token);

        // assert
        var snapshot = new Snapshot();
        await CollectStreamSnapshotData(snapshot, request, result, fusionGraph, cts.Token);
        await snapshot.MatchAsync(cts.Token);
    }

    [Fact]
    public async Task Authors_And_Reviews_Subscription_OnNewReview_Two_Graphs()
    {
        // arrange
        using var cts = new CancellationTokenSource(10_000);
        using var demoProject = await DemoProject.CreateAsync(cts.Token);

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            },
            cts.Token);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsString(fusionGraph))
            .BuildRequestExecutorAsync(cancellationToken: cts.Token);

        var request = Parse(
            """
            subscription OnNewReview {
                onNewReview {
                    body
                    author {
                        name
                        birthdate
                    }
                }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create(),
            cts.Token);

        // assert
        var snapshot = new Snapshot();
        await CollectStreamSnapshotData(snapshot, request, result, fusionGraph, cts.Token);
        await snapshot.MatchAsync(cts.Token);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_ReviewsUser()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsString(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query GetUser {
                a: reviews {
                    body
                    author {
                        name
                    }
                }
                b: reviews {
                    body
                    author {
                        name
                    }
                }
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

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Authors_And_Reviews_Batch_Requests()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsString(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query GetUser {
                reviews {
                    body
                    author {
                        birthdate
                    }
                }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_Query_TopProducts()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsString(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query TopProducts {
                topProducts(first: 2) {
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

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_Query_TypeName()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsString(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query TopProducts {
                __typename
                topProducts(first: 2) {
                    __typename
                    reviews {
                        __typename
                        author {
                            __typename
                        }
                    }
                }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_Introspection()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsString(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query Introspect {
                __schema {
                    types {
                        name
                        kind
                        fields {
                            name
                            type {
                                name
                                kind
                            }
                        }
                    }
                }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();
    }

    private static void CollectSnapshotData(
        Snapshot snapshot,
        DocumentNode request,
        IExecutionResult result,
        Skimmed.Schema fusionGraph)
    {
        snapshot.Add(request, "User Request");

        if (result.ContextData is not null &&
            result.ContextData.TryGetValue("queryPlan", out var value) &&
            value is QueryPlan queryPlan)
        {
            snapshot.Add(queryPlan, "QueryPlan");
        }

        snapshot.Add(result, "Result");
        snapshot.Add(SchemaFormatter.FormatAsString(fusionGraph), "Fusion Graph");
    }

    private static async Task CollectStreamSnapshotData(
        Snapshot snapshot,
        DocumentNode request,
        IExecutionResult result,
        Skimmed.Schema fusionGraph,
        CancellationToken cancellationToken)
    {
        snapshot.Add(request, "User Request");

        var i = 0;
        await foreach(var item in result.ExpectResponseStream()
            .ReadResultsAsync().WithCancellation(cancellationToken))
        {
            if (item.ContextData is not null &&
                item.ContextData.TryGetValue("queryPlan", out var value) &&
                value is QueryPlan queryPlan)
            {
                snapshot.Add(queryPlan, "QueryPlan");
            }

            snapshot.Add(item, $"Result {++i}");
        }

        snapshot.Add(SchemaFormatter.FormatAsString(fusionGraph), "Fusion Graph");
    }

    private const string AccountsExtensionSdl =
        """
        extend type Query {
          userById(id: Int! @is(field: "id")): User!
          usersById(ids: [Int!]! @is(field: "id")): [User!]!
        }
        """;

    private const string ReviewsExtensionSdl =
        """
        extend type Query {
          authorById(id: Int! @is(field: "id")): Author
          productById(upc: Int! @is(field: "upc")): Product
        }

        schema
            @rename(coordinate: "Query.authorById", newName: "userById")
            @rename(coordinate: "Author", newName: "User") {
        }
        """;

    private const string ProductsExtensionSdl =
        """
        extend type Query {
          productById(upc: Int! @is(field: "upc")): Product
        }
        """;
}

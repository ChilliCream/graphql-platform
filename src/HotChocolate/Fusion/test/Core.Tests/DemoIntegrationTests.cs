using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Shared;
using HotChocolate.Fusion.Shared.Shipping;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class DemoIntegrationTests
{
    private readonly Func<ICompositionLog> _logFactory;

    public DemoIntegrationTests(ITestOutputHelper output)
    {
        _logFactory = () => new TestCompositionLog(output);
    }

    [Fact]
    public async Task Authors_And_Reviews_AutoCompose()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        // assert
        SchemaFormatter
            .FormatAsDocument(fusionGraph)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_AutoCompose()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        // assert
        SchemaFormatter
            .FormatAsDocument(fusionGraph)
            .MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserReviews()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
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

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserById()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query GetUser {
              userById(id: "VXNlcgppMQ==") {
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

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserById_With_Invalid_Id_Value()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
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

        Assert.NotNull(result.ExpectQueryResult().Errors);
        Assert.NotEmpty(result.ExpectQueryResult().Errors!);
    }

    [Fact(Skip = "The message order is not guaranteed, this needs to be fixed.")]
    public async Task Authors_And_Reviews_Subscription_OnNewReview()
    {
        // arrange
        using var cts = new CancellationTokenSource(10_000);
        using var demoProject = await DemoProject.CreateAsync(cts.Token);

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            },
            default,
            cts.Token);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
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

    [Fact(Skip = "The message order is not guaranteed, this needs to be fixed.")]
    public async Task Authors_And_Reviews_Subscription_OnNewReview_Two_Graphs()
    {
        // arrange
        using var cts = new CancellationTokenSource(10_000);
        using var demoProject = await DemoProject.CreateAsync(cts.Token);

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            },
            default,
            cts.Token);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
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

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_Reformat_AuthorIds()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
                    },
                    FusionFeatureFlags.NodeField);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query ReformatIds {
                reviews {
                    author {
                        id
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

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact(Skip = "this does not work yet")]
    public async Task Authors_And_Reviews_Query_Reformat_AuthorIds_ReEncodeAllIds()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
                    },
                    FusionFeatureFlags.ReEncodeAllIds);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query ReformatIds {
                reviews {
                    author {
                        id
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

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Authors_And_Reviews_Batch_Requests()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
                    },
                    FusionFeatureFlags.NodeField);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
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

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_Query_TopProducts()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
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

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_Query_TypeName()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
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

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_With_Variables()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query TopProducts($first: Int!) {
                topProducts(first: $first) {
                    id
                }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .SetVariableValue("first", 2)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_Introspection()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
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

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Fetch_User_With_Node_Field()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            },
            FusionFeatureFlags.NodeField);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query FetchNode($id: ID!) {
                node(id: $id) {
                    ... on User {
                        id
                    }
                }
            }
            """);

        var idSerializer = new IdSerializer();
        var id = idSerializer.Serialize("User", 1);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .SetVariableValue("id", id)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Fetch_User_With_Node_Field_Pass_In_Review_Id()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            },
            FusionFeatureFlags.NodeField);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query FetchNode($id: ID!) {
                node(id: $id) {
                    ... on User {
                        id
                    }
                }
            }
            """);

        var idSerializer = new IdSerializer();
        var id = idSerializer.Serialize("Review", 1);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .SetVariableValue("id", id)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Fetch_User_With_Node_Field_Pass_In_Unknown_Id()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            },
            FusionFeatureFlags.NodeField);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query FetchNode($id: ID!) {
                node(id: $id) {
                    ... on User {
                        id
                    }
                }
            }
            """);

        var idSerializer = new IdSerializer();
        var id = idSerializer.Serialize("Unknown", 1);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .SetVariableValue("id", id)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Fetch_User_With_Node_Field_From_Two_Subgraphs()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            },
            FusionFeatureFlags.NodeField);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query FetchNode($id: ID!) {
                node(id: $id) {
                    ... on User {
                        birthdate
                        reviews {
                            body
                        }
                    }
                }
            }
            """);

        var idSerializer = new IdSerializer();
        var id = idSerializer.Serialize("User", 1);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .SetVariableValue("id", id)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Hot_Reload()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();
        var reloadTypeModule = new ReloadTypeModule();


        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                    },
                    FusionFeatureFlags.NodeField);

        var services = new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(_ => new(SchemaFormatter.FormatAsDocument(fusionGraph)))
            .CoreBuilder
            .AddTypeModule(_ => reloadTypeModule)
            .Services
            .BuildServiceProvider();

        var request = Parse(
            """
            {
              __type(name: "Query") {
                fields {
                  name
                }
              }
            }
            """);

        var executorResolver = services.GetRequiredService<IRequestExecutorResolver>();
        var executorProxy = new RequestExecutorProxy(executorResolver, Schema.DefaultName);

        var result = await executorProxy.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        var snapshot = new Snapshot();
        snapshot.Add(result, "1. Version");

        // act
        fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Reviews2.ToConfiguration(AccountsExtensionSdl),
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                    },
                    FusionFeatureFlags.NodeField);

        reloadTypeModule.Evict();

        result = await executorProxy.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        snapshot.Add(result, "2. Version");

        // assert
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task TypeName_Field_On_QueryRoot()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query Introspect {
                __typename
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

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Forward_Nested_Variables()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            },
            FusionFeatureFlags.NodeField);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query ProductReviews(
              $id: ID!
              $first: Int!
            ) {
              productById(id: $id) {
                id
                repeat(num: $first)
              }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .SetVariableValue("id", "UHJvZHVjdAppMQ==")
                .SetVariableValue("first", 1)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Forward_Nested_Node_Variables()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            },
            FusionFeatureFlags.NodeField);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query ProductReviews(
              $id: ID!
              $first: Int!
            ) {
              node(id: $id) {
                ... on Product {
                  id
                  repeat(num: $first)
                }
              }
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .SetVariableValue("id", "UHJvZHVjdAppMQ==")
                .SetVariableValue("first", 1)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Forward_Nested_Object_Variables()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            },
            FusionFeatureFlags.NodeField);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query ProductReviews(
              $id: ID!
              $first: Int!
            ) {
              productById(id: $id) {
                id
                repeatData(data: { data: { num: $first } }) {
                  data {
                    num
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
                .SetVariableValue("id", "UHJvZHVjdAppMQ==")
                .SetVariableValue("first", 1)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    [Fact]
    public async Task Require_Data_In_Context()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl),
            },
            FusionFeatureFlags.NodeField);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query Requires {
                reviews {
                  body
                  author {
                    name
                    birthdate
                  }
                  product {
                    name
                    deliveryEstimate(zip: "12345") {
                      min
                      max
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
                .SetVariableValue("id", "UHJvZHVjdAppMQ==")
                .SetVariableValue("first", 1)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        await snapshot.MatchAsync();

        Assert.Null(result.ExpectQueryResult().Errors);
    }

    private class ReloadTypeModule : TypeModule
    {
        public void Evict() => OnTypesChanged();
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
        snapshot.Add(SchemaFormatter.FormatAsDocument(fusionGraph), "Fusion Graph");
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
        await foreach (var item in result.ExpectResponseStream()
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

        snapshot.Add(SchemaFormatter.FormatAsDocument(fusionGraph), "Fusion Graph");
    }
}

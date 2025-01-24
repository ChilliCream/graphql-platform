using HotChocolate.Execution;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using static HotChocolate.Language.Utf8GraphQLParser;
using static HotChocolate.Fusion.TestHelper;

namespace HotChocolate.Fusion;

public class DemoIntegrationTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

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
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        // assert
        fusionGraph.MatchSnapshot(extension: ".graphql");
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
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        // assert
        fusionGraph.MatchSnapshot(extension: ".graphql");
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
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserReviews_Report_Cost()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync(enableCost: true);

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory)
            .ComposeAsync(
                [
                    demoProject.Reviews2.ToConfiguration(Reviews2ExtensionWithCostSdl),
                    demoProject.Accounts.ToConfiguration(AccountsExtensionWithCostSdl)
                ]);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .ReportCost()
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserReviews_Skip_Author()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query GetUser($skip: Boolean!) {
                users {
                    name
                    reviews {
                        body
                        author @skip(if: $skip) {
                            name
                            birthdate
                        }
                    }
                }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetVariableValues(new Dictionary<string, object?> { { "skip", true } })
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserReviews_Skip_Author_ErrorField()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query GetUser($skip: Boolean!) {
                users {
                    name
                    reviews {
                        body
                        author {
                            name
                            birthdate
                            errorField @skip(if: $skip)
                        }
                    }
                }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetVariableValues(new Dictionary<string, object?> { { "skip", true } })
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query GetUser {
              userById(id: "VXNlcjox") {
                id
              }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.NotNull(result.ExpectOperationResult().Errors);
        Assert.NotEmpty(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public async Task Authors_And_Reviews_Subscription_OnNewReview()
    {
        // arrange
        using var cts = TestEnvironment.CreateCancellationTokenSource();
        using var demoProject = await DemoProject.CreateAsync(cts.Token);

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            },
            default,
            cts.Token);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build(),
            cts.Token);

        // assert
        var snapshot = new Snapshot();
        await CollectStreamSnapshotData(snapshot, request, result, cts.Token);
        await snapshot.MatchMarkdownAsync(cts.Token);
    }

    [Fact]
    public async Task Authors_And_Reviews_Subscription_OnError()
    {
        // arrange
        using var cts = TestEnvironment.CreateCancellationTokenSource();
        using var demoProject = await DemoProject.CreateAsync(cts.Token);

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            },
            default,
            cts.Token);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync(cancellationToken: cts.Token);

        var request = Parse(
            """
            subscription OnError {
                onError {
                    body
                    author {
                        name
                    }
                }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build(),
            cts.Token);

        // assert
        var snapshot = new Snapshot();
        await CollectStreamSnapshotData(snapshot, request, result, cts.Token);
        await snapshot.MatchMarkdownAsync(cts.Token);
    }

    [Fact]
    public async Task Authors_And_Reviews_Subscription_OnError_SSE()
    {
        // arrange
        using var cts = TestEnvironment.CreateCancellationTokenSource();
        using var demoProject = await DemoProject.CreateAsync(cts.Token);

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl, onlyHttp: true),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl, onlyHttp: true),
            },
            default,
            cts.Token);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync(cancellationToken: cts.Token);

        var request = Parse(
            """
            subscription OnError {
                onError {
                    body
                    author {
                        name
                    }
                }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build(),
            cts.Token);

        // assert
        var snapshot = new Snapshot();
        await CollectStreamSnapshotData(snapshot, request, result, cts.Token);
        await snapshot.MatchMarkdownAsync(cts.Token);
    }

    [Fact]
    public async Task Authors_And_Reviews_Subscription_OnNewReview_Two_Graphs()
    {
        // arrange
        using var cts = TestEnvironment.CreateCancellationTokenSource();
        using var demoProject = await DemoProject.CreateAsync(cts.Token);

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            },
            default,
            cts.Token);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build(),
            cts.Token);

        // assert
        var snapshot = new Snapshot();
        await CollectStreamSnapshotData(snapshot, request, result, cts.Token);
        await snapshot.MatchMarkdownAsync(cts.Token);
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
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact(Skip = "Do we want to reformat ids?")]
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
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                    },
                    new FusionFeatureCollection(FusionFeatures.ReEncodeIds));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "first", 2 }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_Introspection()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                [
                    demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                    demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                    demoProject.Products.ToConfiguration(ProductsExtensionSdl)
                ]);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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

        var id = Convert.ToBase64String("User:1"u8);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", id }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task Fetch_User_With_Invalid_Node_Field()
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
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", 1 }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
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
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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

        var id = Convert.ToBase64String("Review:1"u8);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", id }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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

        var id = Convert.ToBase64String("Unknown:1"u8);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", id }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
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
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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

        var id = Convert.ToBase64String("User:1"u8);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", id }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task Hot_Reload()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[] { demoProject.Accounts.ToConfiguration(AccountsExtensionSdl), },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var config = new HotReloadConfiguration(
            new GatewayConfiguration(
                SchemaFormatter.FormatAsDocument(fusionGraph)));

        var services = new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer(null)
            .RegisterGatewayConfiguration(_ => config)
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
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

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
                    new FusionFeatureCollection(FusionFeatures.NodeField));
        config.SetConfiguration(
            new GatewayConfiguration(
                SchemaFormatter.FormatAsDocument(fusionGraph)));

        result = await executorProxy.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        snapshot.Add(result, "2. Version");

        // assert
        await snapshot.MatchMarkdownAsync();
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
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            });

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query Introspect {
                __typename
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", "UHJvZHVjdDox" }, { "first", 1 }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task Forward_Nested_Variables_No_OpName()
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
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query (
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(
                    new Dictionary<string, object?>
                    {
                        { "id", "UHJvZHVjdDox" },
                        { "first", 1 },
                    })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task Forward_Nested_Variables_No_OpName_Two_RootSelections()
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
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query (
              $id: ID!
              $first: Int!
            ) {
              a: productById(id: $id) {
                id
                repeat(num: $first)
              }
              b: productById(id: $id) {
                id
                repeat(num: $first)
              }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", "UHJvZHVjdDox" }, { "first", 1 }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", "UHJvZHVjdDox" }, { "first", 1 }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", "UHJvZHVjdDox" }, { "first", 1 }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
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
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", "UHJvZHVjdDox" }, { "first", 1 }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task Require_Data_In_Context_2()
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
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
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
                    deliveryEstimate(zip: "12345") {
                      min
                      max
                    }
                  }
                }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", "UHJvZHVjdDox" }, { "first", 1 }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task Require_Data_In_Context_3()
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
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query Large {
              users {
                id
                name
                birthdate
                reviews {
                  body
                  author {
                    name
                    birthdate
                  }
                  product {
                    id
                    name
                    deliveryEstimate(zip: "abc") {
                      max
                    }
                  }
                }
              }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", "UHJvZHVjdDox" }, { "first", 1 }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task GetFirstPage_With_After_Null()
    {
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Appointment.ToConfiguration(),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query AfterNull($after: String) {
                appointments(after: $after) {
                   nodes {
                        id
                   }
                }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "after", null }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    [Fact]
    public async Task QueryType_Parallel_Multiple_SubGraphs_WithArguments()
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
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query TopProducts {
              topProducts(first: 5) {
                weight
                deliveryEstimate(zip: "12345") {
                  min
                  max
                }
                reviews {
                    body
                }
              }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "id", "UHJvZHVjdDox" }, { "first", 1 }, })
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();

        Assert.Null(result.ExpectOperationResult().Errors);
    }

    // TODO : FIX THIS TEST
    [Fact(Skip = "This test does not work anymore as it uses CCN which we removed ... ")]
    public async Task ResolveByKey_Handles_Null_Item_Correctly()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Products.ToConfiguration(),
                demoProject.Resale.ToConfiguration(),
            }, new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              viewer {
                # The second product does not exist in the products subgraph
                recommendedResalableProducts {
                  edges {
                    node {
                      product? {
                        id
                        name
                      }
                    }
                  }
                }
              }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
    }

    [Fact]
    public async Task Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node()
    {
        // arrange
        var subgraph1 = await TestSubgraph.CreateAsync(
            """
            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              subgraph1Only: ProductAvailability
            }

            type ProductAvailability implements Node {
              id: ID!
              sharedLinked: ProductAvailabilityMail!
            }

            type ProductAvailabilityMail {
              subgraph1Only: String!
            }

            type Query {
              node("ID of the object." id: ID!): Node
              productById(id: ID!): Product
              productAvailabilityById(id: ID!): ProductAvailability
            }
            """);

        var subgraph2 = await TestSubgraph.CreateAsync(
            """
            interface Node {
              id: ID!
            }

            type ProductAvailability implements Node {
              id: ID!
              sharedLinked: ProductAvailabilityMail!
            }

            type ProductAvailabilityMail {
              subgraph2Only: Boolean!
            }

            type Query {
              node("ID of the object." id: ID!): Node
              productAvailabilityById(id: ID!): ProductAvailability
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph1, subgraph2]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                query($productId: ID!) {
                  productById(id: $productId) {
                    subgraph1Only {
                      sharedLinked {
                        subgraph2Only
                      }
                    }
                  }
                }
                """)
            .SetVariableValues(new Dictionary<string, object?>
            {
                ["productId"] = "UHJvZHVjdAppMzg2MzE4NTk="
            })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node_2()
    {
        // arrange
        var subgraph1 = await TestSubgraph.CreateAsync(
            """
            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              subgraph1Only: ProductAvailability
            }

            type ProductAvailability implements Node {
              id: ID!
              sharedLinked: ProductAvailabilityMail!
            }

            type ProductAvailabilityMail {
              sharedScalar: String!
            }

            type Query {
              node("ID of the object." id: ID!): Node
              productById(id: ID!): Product
              productAvailabilityById(id: ID!): ProductAvailability
            }
            """);

        var subgraph2 = await TestSubgraph.CreateAsync(
            """
            interface Node {
              id: ID!
            }

            type ProductAvailability implements Node {
              sharedLinked: ProductAvailabilityMail!
              subgraph2Only: Boolean!
              id: ID!
            }

            type ProductAvailabilityMail {
              subgraph2Only: Boolean!
              sharedScalar: String!
            }

            type Query {
              node("ID of the object." id: ID!): Node
              productAvailabilityById(id: ID!): ProductAvailability
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph1, subgraph2]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                query($productId: ID!) {
                  productById(id: $productId) {
                    subgraph1Only {
                      subgraph2Only
                      sharedLinked {
                        subgraph2Only
                        sharedScalar
                      }
                    }
                  }
                }
                """)
            .SetVariableValues(new Dictionary<string, object?>
            {
                ["productId"] = "UHJvZHVjdAppMzg2MzE4NTk="
            })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    [Fact]
    public async Task Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node_3()
    {
        // arrange
        var subgraph1 = await TestSubgraph.CreateAsync(
            """
            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              subgraph1Only: ProductAvailability
            }

            type ProductAvailability implements Node {
              id: ID!
              sharedLinked: ProductAvailabilityMail!
              subgraph1Only: Boolean!
            }

            type ProductAvailabilityMail {
              sharedScalar: String!
              subgraph1Only: String!
            }

            type Query {
              node("ID of the object." id: ID!): Node
              productById(id: ID!): Product
              productAvailabilityById(id: ID!): ProductAvailability
            }
            """);

        var subgraph2 = await TestSubgraph.CreateAsync(
            """
            interface Node {
              id: ID!
            }

            type ProductAvailability implements Node {
              sharedLinked: ProductAvailabilityMail!
              subgraph2Only: Boolean!
              id: ID!
            }

            type ProductAvailabilityMail {
              subgraph2Only: Boolean!
              sharedScalar: String!
            }

            type Query {
              node("ID of the object." id: ID!): Node
              productAvailabilityById(id: ID!): ProductAvailability
            }
            """);

        using var subgraphs = new TestSubgraphCollection(output, [subgraph1, subgraph2]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                query($productId: ID!) {
                  productById(id: $productId) {
                    subgraph1Only {
                      subgraph2Only
                      subgraph1Only
                      sharedLinked {
                        subgraph2Only
                        sharedScalar
                        subgraph1Only
                      }
                    }
                  }
                }
                """)
            .SetVariableValues(new Dictionary<string, object?>
            {
                ["productId"] = "UHJvZHVjdAppMzg2MzE4NTk="
            })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        MatchMarkdownSnapshot(request, result);
    }

    // TODO: Test going through node field
    // TODO: Test with multiple levels of shared fields

    public sealed class HotReloadConfiguration : IObservable<GatewayConfiguration>
    {
        private GatewayConfiguration _configuration;
        private Session? _session;

        public HotReloadConfiguration(GatewayConfiguration configuration)
        {
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
        }

        public void SetConfiguration(GatewayConfiguration configuration)
        {
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            _session?.Update();
        }

        public IDisposable Subscribe(IObserver<GatewayConfiguration> observer)
        {
            var session = _session = new Session(this, observer);
            session.Update();
            return session;
        }

        private sealed class Session : IDisposable
        {
            private readonly HotReloadConfiguration _owner;
            private readonly IObserver<GatewayConfiguration> _observer;

            public Session(HotReloadConfiguration owner, IObserver<GatewayConfiguration> observer)
            {
                _owner = owner;
                _observer = observer;
            }

            public void Update()
            {
                _observer.OnNext(_owner._configuration);
            }

            public void Dispose() { }
        }
    }
}

using System.Net;
using CookieCrumble;
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

// TODO: Two errors pointing to the same field is not spec-compliant.

namespace HotChocolate.Fusion;

public class ErrorTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Resolve_ResolveByKey_Sequence_Second_Service_Offline_Field_Nullable()
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
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Accounts.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query {
              reviews {
                body
                author? {
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
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_ResolveByKey_Sequence_Second_Service_Offline_Field_NonNull()
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
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Accounts.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query {
              reviews {
                body
                author! {
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
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_ResolveByKey_Sequence_Second_Service_Offline_List_Item_Nullable()
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
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Accounts.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query {
              reviews[?]! {
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
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_ResolveByKey_Sequence_Second_Service_Offline_Entry_Field_Nullable()
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
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Accounts.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query {
              reviews? {
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
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Service_Offline_Entry_Field_Nullable()
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
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Reviews2.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              reviewById(id: "UmV2aWV3Cmkx")? {
                body
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
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Followed_By_Parallel_Resolve_One_Service_Offline_Field_Nullable()
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
                        demoProject.Products.ToConfiguration(ProductsExtensionSdl),
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Products.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              reviewById(id: "UmV2aWV3Cmkx")? {
                body
                author {
                  username
                }
                product? {
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
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Followed_By_Parallel_Resolve_One_Service_Offline_Field_NonNull()
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
                        demoProject.Products.ToConfiguration(ProductsExtensionSdl),
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Products.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              reviewById(id: "UmV2aWV3Cmkx")? {
                body
                author {
                  username
                }
                product! {
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
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Service_Offline_Entry_Field_NonNull()
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
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Reviews2.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              reviewById(id: "UmV2aWV3Cmkx")! {
                body
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
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Sequence_Second_Service_Offline_Field_Nullable()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                        demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Accounts.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              reviewById(id: "UmV2aWV3Cmkx") {
                body
                author? {
                  username
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
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Sequence_Second_Service_Offline_Field_NonNull()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                        demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Accounts.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              reviewById(id: "UmV2aWV3Cmkx") {
                body
                author! {
                  username
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
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Parallel_One_Service_Offline_Sub_Field_On_Shared_Entry_Type_Nullable()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Accounts.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              viewer {
                user {
                  name
                }
                latestReview {
                  body
                }
              }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Resolve_Parallel_One_Service_Offline_Sub_Field_On_Shared_Entry_Type_NonNull()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory, demoProject.Accounts.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              viewer? {
                user! {
                  name
                }
                latestReview {
                  body
                }
              }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        snapshot.MatchMarkdownSnapshot();
    }


    [Fact]
    public async Task Resolve_Parallel_Both_Services_Offline_Shared_Entry_Field_Nullable()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(
                new ErrorFactory(demoProject.HttpClientFactory,
                    demoProject.Accounts.Name,
                    demoProject.Reviews2.Name))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              viewer? {
                user {
                  name
                }
                latestReview {
                  body
                }
              }
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Subgraph_Error_Top_Level_Field()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[] { demoProject.Accounts.ToConfiguration(AccountsExtensionSdl), },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

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
                data {
                  accountValue
                }
              }
              errorField
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
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Subgraph_Error_For_Field_Resolved_In_Sequence()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                        demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
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
            {
              reviewById(id: "UmV2aWV3Cmkx") {
                body
                author {
                  username
                  errorField
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
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Subgraph_Error_For_Field_Within_A_List()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                        demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
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
            {
              userById(id: "VXNlcgppMQ==") {
                account1: birthdate
                account2: birthdate
                username
                reviews {
                  body
                  errorField
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
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Subgraph_Error_For_Field_Resolved_Via_Key_Batch()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph =
            await new FusionGraphComposer(logFactory: _logFactory)
                .ComposeAsync(
                    new[]
                    {
                        demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                        demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
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
            {
              reviews {
                body
                author {
                  id
                  errorField
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
        snapshot.MatchMarkdownSnapshot();
    }

    private class ErrorFactory(IHttpClientFactory innerFactory, params string[] errorClients)
        : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            if (errorClients.Contains(name, StringComparer.Ordinal))
            {
                var client = new HttpClient(new ErrorHandler());
                return client;
            }

            return innerFactory.CreateClient(name);
        }

        private class ErrorHandler : HttpClientHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }
}

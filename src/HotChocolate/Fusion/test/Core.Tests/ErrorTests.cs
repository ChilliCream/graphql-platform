using System.Net;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using static HotChocolate.Language.Utf8GraphQLParser;
using static HotChocolate.Fusion.TestHelper;

namespace HotChocolate.Fusion;

public class ErrorTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task TopLevelResolveSubgraphError()
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
                    },
                    new FusionFeatureCollection(FusionFeatures.NodeField));

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .ModifyFusionOptions(options => options.IncludeDebugInfo = true)
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
            OperationRequestBuilder
                .Create()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task NestedResolveSubgraphError()
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
            .ModifyFusionOptions(options => options.IncludeDebugInfo = true)
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              reviewById(id: "UmV2aWV3OjE=") {
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
            OperationRequestBuilder
                .Create()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task NestedResolveWithListSubgraphError()
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
            .ModifyFusionOptions(options => options.IncludeDebugInfo = true)
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            {
              userById(id: "VXNlcjox") {
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
            OperationRequestBuilder
                .Create()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ResolveByKeySubgraphError()
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
            .ModifyFusionOptions(options => options.IncludeDebugInfo = true)
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
            OperationRequestBuilder
                .Create()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Accounts_Offline_Author_Nullable()
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
            query ReformatIds {
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
            OperationRequestBuilder
                .Create()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Accounts_Offline_Author_NonNull()
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
            query ReformatIds {
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
            OperationRequestBuilder
                .Create()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Accounts_Offline_Reviews_ListElement_Nullable()
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
            query ReformatIds {
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
            OperationRequestBuilder
                .Create()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result, fusionGraph);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Internal_Server_Error_On_Root_Field()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(new ErrorFactory(demoProject.HttpClientFactory, "a"))
            .AddSingleton(demoProject.WebSocketConnectionFactory)
            .AddFusionGatewayServer()
            .ConfigureFromDocument(
                Parse(
                    """
                    schema
                      @fusion(version: 1)
                      @transport(subgraph: "a", group: "a", location: "http:\/\/localhost\/graphql", kind: "HTTP") {
                      query: Query
                      mutation: Mutation
                    }

                    type Query {
                      a: Boolean!
                        @resolver(subgraph: "a", select: "{ a }")
                    }
                    """))
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync();

        var request = Parse(
            """
            query A {
                a
            }
            """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .Create()
                .SetDocument(request)
                .Build());

        // assert
        result.MatchSnapshot();
    }

    private class ErrorFactory : IHttpClientFactory
    {
        private readonly IHttpClientFactory _innerFactory;
        private readonly string _errorClient;

        public ErrorFactory(IHttpClientFactory innerFactory, string errorClient)
        {
            _innerFactory = innerFactory;
            _errorClient = errorClient;
        }

        public HttpClient CreateClient(string name)
        {
            if (_errorClient.EqualsOrdinal(name))
            {
                var client = new HttpClient(new ErrorHandler());
                return client;
            }

            return _innerFactory.CreateClient(name);
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

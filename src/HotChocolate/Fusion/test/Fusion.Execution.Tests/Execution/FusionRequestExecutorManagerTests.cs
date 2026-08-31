using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class FusionRequestExecutorManagerTests : FusionTestBase
{
    private const string DefaultAcceptHeader =
        "application/graphql-response+json; charset=utf-8, application/json; charset=utf-8, "
        + "application/jsonl; charset=utf-8, text/event-stream; charset=utf-8";

    private const string BatchingAcceptHeader =
        "application/jsonl; charset=utf-8, text/event-stream; charset=utf-8, "
        + "application/graphql-response+json; charset=utf-8, application/json; charset=utf-8";

    private const string SubscriptionAcceptHeader =
        "application/jsonl; charset=utf-8, text/event-stream; charset=utf-8";

    [Fact]
    public async Task GetExecutorAsync_Throws_If_Schema_Does_Not_Exist()
    {
        // arrange
        var schemaDocument =
            ComposeSchemaDocument(
                """
                type Query {
                    foo: String
                }
                """);

        var manager =
            new ServiceCollection()
                .AddGraphQLGateway("some-name")
                .AddInMemoryConfiguration(schemaDocument)
                .Services
                .BuildServiceProvider()
                .GetRequiredService<FusionRequestExecutorManager>();

        // act
        var act = async () => await manager.GetExecutorAsync("unknown-name");

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal("The requested schema 'unknown-name' does not exist.", exception.Message);
    }

    [Fact]
    public async Task Create_Executor()
    {
        // arrange
        var schemaDocument =
            ComposeSchemaDocument(
                """
                type Query {
                    foo: String
                }
                """);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(schemaDocument)
                .UseDefaultPipeline()
                .Services
                .BuildServiceProvider();

        // act
        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executor = await executorProvider.GetExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(ISchemaDefinition.DefaultName, executor.Schema.Name);
    }

    [Fact]
    public async Task Get_Plan_From_Execution_Result()
    {
        // arrange
        var schemaDocument =
            ComposeSchemaDocument(
                """
                type Query {
                    foo: String
                }
                """);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(schemaDocument)
                .UseDefaultPipeline()
                .UseRequest(
                    (_, _) =>
                    {
                        return context =>
                        {
                            var plan = context.GetOperationPlan();
                            context.Result =
                                new OperationResult(
                                    ImmutableOrderedDictionary<string, object?>.Empty.Add("operationPlan", plan));
                            return ValueTask.CompletedTask;
                        };
                    },
                    before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
                    allowMultiple: true)
                .Services
                .BuildServiceProvider();

        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executor = await executorProvider.GetExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query Test {
                        foo
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.True(operationResult.Extensions.TryGetValue("operationPlan", out var operationPlan));
        Assert.NotNull(operationPlan);
        Assert.Equal("Test", Assert.IsType<OperationPlan>(operationPlan).OperationName);
    }

    [Fact]
    public async Task Executor_Should_Only_Be_Switched_Once_It_Is_Warmed_Up()
    {
        // arrange
        var warmupResetEvent = new ManualResetEventSlim(true);
        var executorEvictedResetEvent = new ManualResetEventSlim(false);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var configProvider = new TestFusionConfigurationProvider(CreateConfiguration());

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .AddWarmupTask((_, _) =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    warmupResetEvent.Wait(cts.Token);

                    return Task.CompletedTask;
                })
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        manager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Evicted)
            {
                executorEvictedResetEvent.Set();
            }
        }));

        // act
        // assert
        var initialExecutor = await manager.GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        warmupResetEvent.Reset();

        configProvider.UpdateConfiguration(
            CreateConfiguration(
                """
                type Query {
                  field2: String!
                }
                """));

        var executorAfterEviction = await manager.GetExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(initialExecutor, executorAfterEviction);

        warmupResetEvent.Set();
        executorEvictedResetEvent.Wait(cts.Token);
        var executorAfterWarmup = await manager.GetExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotSame(initialExecutor, executorAfterWarmup);

        cts.Dispose();
    }

    [Fact]
    public async Task WarmupTasks_Are_Applied_Correct_Number_Of_Times()
    {
        // arrange
        var warmups = 0;
        var executorEvictedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var configProvider = new TestFusionConfigurationProvider(CreateConfiguration());

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .AddWarmupTask((_, _) =>
                {
                    warmups++;
                    return Task.CompletedTask;
                })
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        manager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Evicted)
            {
                executorEvictedResetEvent.Set();
            }
        }));

        // act
        // assert
        var initialExecutor = await manager.GetExecutorAsync(cancellationToken: cts.Token);

        Assert.Equal(1, warmups);

        configProvider.UpdateConfiguration(
            CreateConfiguration(
                """
                type Query {
                  field2: String!
                }
                """));
        executorEvictedResetEvent.Wait(cts.Token);

        var executorAfterEviction = await manager.GetExecutorAsync(cancellationToken: cts.Token);

        Assert.NotSame(initialExecutor, executorAfterEviction);
        Assert.Equal(2, warmups);
    }

    [Fact]
    public async Task Executor_Should_NotRebuild_When_PolicyContentChangesIncludingResourceRequirements()
    {
        // arrange
        var evictions = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var policyContentSink = new PolicyContentSink();

        var configProvider = new TestFusionConfigurationProvider(
            CreateConfigurationWithPolicy("{ id }", "grant-v1"u8));

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .ConfigureSchemaServices(
                    (_, schemaServices) => schemaServices.AddSingleton<IPolicyProvider>(policyContentSink))
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        manager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Evicted)
            {
                Interlocked.Increment(ref evictions);
            }
        }));

        var initialExecutor = await manager.GetExecutorAsync(cancellationToken: cts.Token);
        var initialContentDelivered = ReferenceEquals(
            configProvider.Configuration!.Policies,
            policyContentSink.Current);

        // act
        // A rego source-only change keeps the schema and settings unchanged, so it is adopted
        // without rebuilding the executor and delivered directly to the current provider.
        configProvider.UpdateConfiguration(
            CreateConfigurationWithPolicy("{ id }", "grant-v2"u8));

        // A resource requirements change also leaves the schema and settings unchanged. It is no
        // longer a rebuild trigger: it is handled by targeted plan-cache eviction inside
        // PolicyCollection, adopted the same way as a source-only change.
        var finalConfiguration = CreateConfigurationWithPolicy("{ id name }", "grant-v2"u8);
        var delivery = policyContentSink.Expect(finalConfiguration.Policies);
        configProvider.UpdateConfiguration(finalConfiguration);

        await delivery.WaitAsync(cts.Token);

        var executorAfterChange = await manager.GetExecutorAsync(cancellationToken: cts.Token);

        // assert
        // Neither change rebuilt the executor, so no eviction occurred and the same instance is
        // still served.
        Assert.Equal(
            (
                Evictions: 0,
                SameExecutor: true,
                InitialContentDelivered: true,
                DeliveredContent: finalConfiguration.Policies),
            (
                Evictions: evictions,
                SameExecutor: ReferenceEquals(initialExecutor, executorAfterChange),
                InitialContentDelivered: initialContentDelivered,
                DeliveredContent: policyContentSink.Current));
    }

    private sealed class PolicyContentSink
        : IPolicyProvider
        , IObserver<PolicyContentSnapshot?>
    {
        private readonly object _sync = new();
        private TaskCompletionSource<PolicyContentSnapshot?>? _expected;
        private PolicyContentSnapshot? _expectedContent;

        public PolicyContentSnapshot? Current { get; private set; }

        public Task<PolicyContentSnapshot?> Expect(PolicyContentSnapshot? content)
        {
            lock (_sync)
            {
                _expected = new TaskCompletionSource<PolicyContentSnapshot?>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                _expectedContent = content;

                if (ReferenceEquals(Current, content))
                {
                    _expected.TrySetResult(content);
                }

                return _expected.Task;
            }
        }

        public void OnNext(PolicyContentSnapshot? value)
        {
            lock (_sync)
            {
                Current = value;

                if (_expected is { } expected
                    && ReferenceEquals(_expectedContent, value))
                {
                    expected.TrySetResult(value);
                }
            }
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }

        public IDisposable Subscribe(IObserver<ImmutableArray<IPolicy>> observer)
        {
            observer.OnNext([]);
            return EmptyDisposable.Instance;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private sealed class EmptyDisposable : IDisposable
        {
            public static EmptyDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }

    private static FusionConfiguration CreateConfigurationWithPolicy(
        string requirements,
        ReadOnlySpan<byte> source)
    {
        var policy = new PolicyContent(
            "CanReadProduct.allow",
            PolicyContentType.Rego,
            source.ToArray(),
            new PolicyRequirements
            {
                Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet(requirements)
            },
            "digest"u8.ToArray());

        var snapshot = new PolicyContentSnapshot(
            "rego",
            new Version(1, 0, 0),
            [policy],
            default,
            default,
            dataOwner: null);

        return CreateFusionConfiguration("type Query { field: String! }") with { Policies = snapshot };
    }

    [Fact]
    public async Task Calling_GetExecutorAsync_Multiple_Times_Only_Creates_One_Executor()
    {
        // arrange
        var configProvider = new TestFusionConfigurationProvider(CreateConfiguration());

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        // act
        var executor1Task = Task.Run(async () => await manager.GetExecutorAsync());
        var executor2Task = Task.Run(async () => await manager.GetExecutorAsync());

        var executor1 = await executor1Task;
        var executor2 = await executor2Task;

        // assert
        Assert.Same(executor1, executor2);
    }

    [Fact]
    public async Task Executor_Resolution_Should_Be_Parallel()
    {
        // arrange
        var schema1CreationResetEvent = new ManualResetEventSlim(false);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var configProvider = new TestFusionConfigurationProvider(CreateConfiguration());

        var services = new ServiceCollection();
        services
            .AddGraphQLGateway("schema1")
            .AddConfigurationProvider(_ => configProvider)
            .ConfigureSchemaServices((_, _) =>
            {
                // This is just here to block during the executor creation.
                schema1CreationResetEvent.Wait(cts.Token);
            });
        services
            .AddGraphQLGateway("schema2")
            .AddConfigurationProvider(_ => configProvider);

        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<FusionRequestExecutorManager>();

        // act
        var executor1Task = Task.Run(async () => await manager.GetExecutorAsync("schema1"), cts.Token);
        var executor2Task = Task.Run(async () => await manager.GetExecutorAsync("schema2"), cts.Token);

        // assert
        await executor2Task;

        schema1CreationResetEvent.Set();

        await executor1Task;

        Assert.NotEqual(executor1Task, executor2Task);

        cts.Dispose();
    }

    [Fact]
    public async Task WarmupTask_Should_Be_Able_To_Access_Schema_And_Regular_Services()
    {
        // arrange
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var services = new ServiceCollection();
        services.AddSingleton<SomeService>();
        services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(CreateConfiguration().Schema)
            .AddApplicationService<SomeService>()
            .AddWarmupTask<CustomWarmupTask>();
        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<FusionRequestExecutorManager>();

        // act
        var executor = await manager.GetExecutorAsync(cancellationToken: cts.Token);

        // assert
        Assert.NotNull(executor);

        cts.Dispose();
    }

    [Fact]
    public async Task CreateHttpClientConfiguration_Should_UseDefaults_When_NoCapabilitiesSpecified()
    {
        // arrange
        var config = CreateConfigurationWithSettings(
            """
            {
                "sourceSchemas": {
                    "a": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql"
                            }
                        }
                    }
                }
            }
            """);

        var configProvider = new TestFusionConfigurationProvider(config);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        // act
        var executor = await manager.GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var clientConfigs = executor.Schema.Features.GetRequired<SourceSchemaClientConfigurations>();
        Assert.True(clientConfigs.TryGet("a", OperationType.Query, out var queryConfig));
        Assert.True(clientConfigs.TryGet("a", OperationType.Mutation, out _));
        Assert.True(clientConfigs.TryGet("a", OperationType.Subscription, out _));

        var httpConfig = Assert.IsType<HttpSourceSchemaClientConfiguration>(queryConfig);
        Assert.Equal("a", httpConfig.Name);
        Assert.Equal(HttpSourceSchemaClientConfiguration.DefaultClientName, httpConfig.HttpClientName);
        Assert.Equal(new Uri("http://localhost:5000/graphql"), httpConfig.BaseAddress);
        Assert.Equal(SourceSchemaClientCapabilities.Default, httpConfig.Capabilities);
        Assert.Equal(SupportedOperationType.All, httpConfig.SupportedOperations);
        Assert.Equal(DefaultAcceptHeader, httpConfig.DefaultAcceptHeaderValue);
        Assert.Equal(BatchingAcceptHeader, httpConfig.BatchingAcceptHeaderValue);
        Assert.Equal(SubscriptionAcceptHeader, httpConfig.SubscriptionAcceptHeaderValue);
    }

    [Fact]
    public async Task CreateHttpClientConfiguration_Should_UseCustomClientName_When_Specified()
    {
        // arrange
        var config = CreateConfigurationWithSettings(
            """
            {
                "sourceSchemas": {
                    "a": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "clientName": "my-custom-client"
                            }
                        }
                    }
                }
            }
            """);

        var configProvider = new TestFusionConfigurationProvider(config);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        // act
        var executor = await manager.GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var clientConfigs = executor.Schema.Features.GetRequired<SourceSchemaClientConfigurations>();
        Assert.True(clientConfigs.TryGet("a", OperationType.Query, out var queryConfig));

        var httpConfig = Assert.IsType<HttpSourceSchemaClientConfiguration>(queryConfig);
        Assert.Equal("a", httpConfig.Name);
        Assert.Equal("my-custom-client", httpConfig.HttpClientName);
        Assert.Equal(new Uri("http://localhost:5000/graphql"), httpConfig.BaseAddress);
        Assert.Equal(SourceSchemaClientCapabilities.Default, httpConfig.Capabilities);
        Assert.Equal(SupportedOperationType.All, httpConfig.SupportedOperations);
        Assert.Equal(DefaultAcceptHeader, httpConfig.DefaultAcceptHeaderValue);
        Assert.Equal(BatchingAcceptHeader, httpConfig.BatchingAcceptHeaderValue);
        Assert.Equal(SubscriptionAcceptHeader, httpConfig.SubscriptionAcceptHeaderValue);
    }

    [Fact]
    public async Task CreateHttpClientConfiguration_Should_DisableVariableBatching_When_SetToFalse()
    {
        // arrange
        var config = CreateConfigurationWithSettings(
            """
            {
                "sourceSchemas": {
                    "a": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "capabilities": {
                                    "batching": {
                                        "variableBatching": false
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """);

        var configProvider = new TestFusionConfigurationProvider(config);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        // act
        var executor = await manager.GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var clientConfigs = executor.Schema.Features.GetRequired<SourceSchemaClientConfigurations>();
        Assert.True(clientConfigs.TryGet("a", OperationType.Query, out var queryConfig));

        var httpConfig = Assert.IsType<HttpSourceSchemaClientConfiguration>(queryConfig);
        Assert.Equal("a", httpConfig.Name);
        Assert.Equal(HttpSourceSchemaClientConfiguration.DefaultClientName, httpConfig.HttpClientName);
        Assert.Equal(new Uri("http://localhost:5000/graphql"), httpConfig.BaseAddress);
        Assert.Equal(SourceSchemaClientCapabilities.RequestBatching, httpConfig.Capabilities);
        Assert.Equal(SupportedOperationType.All, httpConfig.SupportedOperations);
        Assert.Equal(DefaultAcceptHeader, httpConfig.DefaultAcceptHeaderValue);
        Assert.Equal(BatchingAcceptHeader, httpConfig.BatchingAcceptHeaderValue);
        Assert.Equal(SubscriptionAcceptHeader, httpConfig.SubscriptionAcceptHeaderValue);
    }

    [Fact]
    public async Task CreateHttpClientConfiguration_Should_DisableRequestBatching_When_SetToFalse()
    {
        // arrange
        var config = CreateConfigurationWithSettings(
            """
            {
                "sourceSchemas": {
                    "a": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "capabilities": {
                                    "batching": {
                                        "requestBatching": false
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """);

        var configProvider = new TestFusionConfigurationProvider(config);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        // act
        var executor = await manager.GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var clientConfigs = executor.Schema.Features.GetRequired<SourceSchemaClientConfigurations>();
        Assert.True(clientConfigs.TryGet("a", OperationType.Query, out var queryConfig));

        var httpConfig = Assert.IsType<HttpSourceSchemaClientConfiguration>(queryConfig);
        Assert.Equal("a", httpConfig.Name);
        Assert.Equal(HttpSourceSchemaClientConfiguration.DefaultClientName, httpConfig.HttpClientName);
        Assert.Equal(new Uri("http://localhost:5000/graphql"), httpConfig.BaseAddress);
        Assert.Equal(SourceSchemaClientCapabilities.VariableBatching, httpConfig.Capabilities);
        Assert.Equal(SupportedOperationType.All, httpConfig.SupportedOperations);
        Assert.Equal(DefaultAcceptHeader, httpConfig.DefaultAcceptHeaderValue);
        Assert.Equal(BatchingAcceptHeader, httpConfig.BatchingAcceptHeaderValue);
        Assert.Equal(SubscriptionAcceptHeader, httpConfig.SubscriptionAcceptHeaderValue);
    }

    [Fact]
    public async Task CreateHttpClientConfiguration_Should_DisableSubscriptions_When_NotSupported()
    {
        // arrange
        var config = CreateConfigurationWithSettings(
            """
            {
                "sourceSchemas": {
                    "a": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "capabilities": {
                                    "subscriptions": {
                                        "supported": false
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """);

        var configProvider = new TestFusionConfigurationProvider(config);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        // act
        var executor = await manager.GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var clientConfigs = executor.Schema.Features.GetRequired<SourceSchemaClientConfigurations>();
        Assert.True(clientConfigs.TryGet("a", OperationType.Query, out var queryConfig));

        var httpConfig = Assert.IsType<HttpSourceSchemaClientConfiguration>(queryConfig);
        Assert.Equal("a", httpConfig.Name);
        Assert.Equal(HttpSourceSchemaClientConfiguration.DefaultClientName, httpConfig.HttpClientName);
        Assert.Equal(new Uri("http://localhost:5000/graphql"), httpConfig.BaseAddress);
        Assert.Equal(SourceSchemaClientCapabilities.Default, httpConfig.Capabilities);
        Assert.Equal(SupportedOperationType.Query | SupportedOperationType.Mutation, httpConfig.SupportedOperations);
        Assert.Equal(DefaultAcceptHeader, httpConfig.DefaultAcceptHeaderValue);
        Assert.Equal(BatchingAcceptHeader, httpConfig.BatchingAcceptHeaderValue);
        Assert.Equal(SubscriptionAcceptHeader, httpConfig.SubscriptionAcceptHeaderValue);

        Assert.False(clientConfigs.TryGet("a", OperationType.Subscription, out _));
    }

    [Fact]
    public async Task CreateHttpClientConfiguration_Should_UseCustomFormats_When_Specified()
    {
        // arrange
        var config = CreateConfigurationWithSettings(
            """
            {
                "sourceSchemas": {
                    "a": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "capabilities": {
                                    "standard": {
                                        "formats": ["application/json", "text/plain"]
                                    },
                                    "batching": {
                                        "formats": ["application/jsonl"]
                                    },
                                    "subscriptions": {
                                        "formats": ["text/event-stream"]
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """);

        var configProvider = new TestFusionConfigurationProvider(config);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        // act
        var executor = await manager.GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var clientConfigs = executor.Schema.Features.GetRequired<SourceSchemaClientConfigurations>();
        Assert.True(clientConfigs.TryGet("a", OperationType.Query, out var queryConfig));

        var httpConfig = Assert.IsType<HttpSourceSchemaClientConfiguration>(queryConfig);
        Assert.Equal("a", httpConfig.Name);
        Assert.Equal(HttpSourceSchemaClientConfiguration.DefaultClientName, httpConfig.HttpClientName);
        Assert.Equal(new Uri("http://localhost:5000/graphql"), httpConfig.BaseAddress);
        Assert.Equal(SourceSchemaClientCapabilities.Default, httpConfig.Capabilities);
        Assert.Equal(SupportedOperationType.All, httpConfig.SupportedOperations);
        Assert.Equal("application/json, text/plain", httpConfig.DefaultAcceptHeaderValue);
        Assert.Equal("application/jsonl", httpConfig.BatchingAcceptHeaderValue);
        Assert.Equal("text/event-stream", httpConfig.SubscriptionAcceptHeaderValue);
    }

#pragma warning disable CS9113 // Parameter is unread.
    private sealed class CustomWarmupTask(IDocumentCache documentCache, SomeService service) : IRequestExecutorWarmupTask
#pragma warning restore CS9113 // Parameter is unread.
    {
        public bool ApplyOnlyOnStartup => false;

        public Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class SomeService;

    private static FusionConfiguration CreateConfiguration(string? sourceSchemaText = null)
    {
        sourceSchemaText ??=
            """
            type Query {
              field: String!
            }
            """;

        return CreateFusionConfiguration(sourceSchemaText);
    }

    private static FusionConfiguration CreateConfigurationWithSettings(string settingsJson)
    {
        var compositeSchema = ComposeSchemaDocument("type Query { foo: String }");
        var settings = JsonDocument.Parse(settingsJson);

        return new FusionConfiguration(
            compositeSchema,
            new JsonDocumentOwner(settings));
    }
}

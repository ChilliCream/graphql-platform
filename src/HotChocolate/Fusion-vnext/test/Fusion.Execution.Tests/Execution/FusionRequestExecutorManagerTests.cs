using System.Buffers;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class FusionRequestExecutorManagerTests : FusionTestBase
{
    [Fact]
    public async Task GetExecutorAsync_Throws_If_Schema_Does_Not_Exist()
    {
        // arrange
        var schemaDocument =
            ComposeSchemaDocument(
                """
                schema {
                    query: Query
                }

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
        Assert.Equal($"The requested schema 'unknown-name' does not exist.", exception.Message);
    }

    [Fact]
    public async Task CreateExecutor()
    {
        // arrange
        var schemaDocument =
            ComposeSchemaDocument(
                """
                schema {
                    query: Query
                }

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
        var executor = await executorProvider.GetExecutorAsync();

        // assert
        Assert.Equal(ISchemaDefinition.DefaultName, executor.Schema.Name);
    }

    [Fact]
    public async Task GetOperationPlanFromExecution()
    {
        // arrange
        var schemaDocument =
            ComposeSchemaDocument(
                """
                schema {
                    query: Query
                }

                type Query {
                    foo: String
                }
                """);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(schemaDocument)
                .UseDefaultPipeline()
                .InsertUseRequest(
                    before: nameof(OperationExecutionMiddleware),
                    (_, _) =>
                    {
                        return context =>
                        {
                            var plan = context.GetOperationPlan();
                            context.Result =
                                OperationResultBuilder.New()
                                    .SetData(
                                        new Dictionary<string, object?>
                                        {
                                            { "foo", null }
                                        })
                                    .SetContextData(
                                        new Dictionary<string, object?>
                                        {
                                            { "operationPlan", plan }
                                        })
                                        .Build();
                            return ValueTask.CompletedTask;
                        };
                    })
                .Services
                .BuildServiceProvider();

        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executor = await executorProvider.GetExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query Test {
                        foo
                    }
                    """)
                .Build());

        // assert
        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData.TryGetValue("operationPlan", out var operationPlan));
        Assert.NotNull(operationPlan);
        Assert.Equal("Test", Assert.IsType<OperationPlan>(operationPlan).OperationName);
    }

    [Fact]
    public async Task Plan_Cache_Should_Be_Scoped_To_Executor()
    {
        // arrange
        var executorEvictedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var configProvider = new TestFusionConfigurationProvider(CreateConfiguration());

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
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
        var firstExecutor = await manager.GetExecutorAsync(cancellationToken: cts.Token);
        var firstPlanCache = firstExecutor.Schema.Services.GetCombinedServices()
            .GetRequiredService<Cache<OperationPlan>>();

        configProvider.UpdateConfiguration(
            CreateConfiguration(
                """
                schema {
                  query: Query
                }

                type Query {
                  field2: String!
                }
                """));
        executorEvictedResetEvent.Wait(cts.Token);

        var secondExecutor = await manager.GetExecutorAsync(cancellationToken: cts.Token);
        var secondPlanCache = secondExecutor.Schema.Services.GetCombinedServices()
            .GetRequiredService<Cache<OperationPlan>>();

        // assert
        Assert.NotSame(secondPlanCache, firstPlanCache);
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
        var initialExecutor = await manager.GetExecutorAsync();
        warmupResetEvent.Reset();

        configProvider.UpdateConfiguration(
            CreateConfiguration(
                """
                schema {
                  query: Query
                }

                type Query {
                  field2: String!
                }
                """));

        var executorAfterEviction = await manager.GetExecutorAsync();

        Assert.Same(initialExecutor, executorAfterEviction);

        warmupResetEvent.Set();
        executorEvictedResetEvent.Wait(cts.Token);
        var executorAfterWarmup = await manager.GetExecutorAsync();

        Assert.NotSame(initialExecutor, executorAfterWarmup);

        cts.Dispose();
    }

    [Fact]
    public async Task WarmupSchemaTasks_Are_Applied_Correct_Number_Of_Times()
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
                schema {
                  query: Query
                }

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

    private static FusionConfiguration CreateConfiguration(string? sourceSchemaText = null)
    {
        sourceSchemaText ??=
            """
            schema {
              query: Query
            }

            type Query {
              field: String!
            }
            """;

        var schema = ComposeSchemaDocument(sourceSchemaText);

        return new FusionConfiguration(
            schema,
            new JsonDocumentOwner(
                JsonDocument.Parse("{ }"),
                new EmptyMemoryOwner()));
    }

    private sealed class TestFusionConfigurationProvider(FusionConfiguration initialConfig) : IFusionConfigurationProvider
    {
        private List<IObserver<FusionConfiguration>> _observers = [];

        public IDisposable Subscribe(IObserver<FusionConfiguration> observer)
        {
            if (Configuration is not null)
            {
                observer.OnNext(Configuration);
            }

            _observers.Add(observer);

            return new Observer();
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public FusionConfiguration? Configuration { get; private set; } = initialConfig;

        public void UpdateConfiguration(FusionConfiguration configuration)
        {
            Configuration = configuration;

            foreach (var observer in _observers)
            {
                observer.OnNext(Configuration);
            }
        }

        private sealed class Observer : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private class EmptyMemoryOwner : IMemoryOwner<byte>
    {
        public Memory<byte> Memory => default;

        public void Dispose() { }
    }
}

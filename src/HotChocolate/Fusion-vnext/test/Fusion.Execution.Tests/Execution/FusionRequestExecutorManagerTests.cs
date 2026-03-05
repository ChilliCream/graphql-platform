using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
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
        var executor = await executorProvider.GetExecutorAsync();

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
                .InsertUseRequest(
                    before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
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
        var initialExecutor = await manager.GetExecutorAsync();
        warmupResetEvent.Reset();

        configProvider.UpdateConfiguration(
            CreateConfiguration(
                """
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
}

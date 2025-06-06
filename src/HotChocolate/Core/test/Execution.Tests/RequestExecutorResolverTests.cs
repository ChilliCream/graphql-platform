using HotChocolate.Execution.Caching;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class RequestExecutorResolverTests
{
    [Fact]
    public async Task Operation_Cache_Should_Be_Scoped_To_Executor()
    {
        // arrange
        var executorEvictedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var manager = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve(""))
            .Services.BuildServiceProvider()
            .GetRequiredService<RequestExecutorManager>();

        manager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Evicted)
            {
                executorEvictedResetEvent.Set();
            }
        }));

        // act
        var firstExecutor = await manager.GetExecutorAsync();
        var firstOperationCache = firstExecutor.Schema.Services.GetCombinedServices()
            .GetRequiredService<IPreparedOperationCache>();

        manager.EvictRequestExecutor();
        executorEvictedResetEvent.Wait(cts.Token);

        var secondExecutor = await manager.GetExecutorAsync();
        var secondOperationCache = secondExecutor.Schema.Services.GetCombinedServices()
            .GetRequiredService<IPreparedOperationCache>();

        // assert
        Assert.NotSame(secondOperationCache, firstOperationCache);
    }

    [Fact]
    public async Task Executor_Should_Only_Be_Switched_Once_It_Is_Warmed_Up()
    {
        // arrange
        var warmupResetEvent = new ManualResetEventSlim(true);
        var executorEvictedResetEvent = new ManualResetEventSlim(false);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var manager = new ServiceCollection()
            .AddGraphQL()
            .InitializeOnStartup(
                keepWarm: true,
                warmup: (_, _) =>
                {
                    warmupResetEvent.Wait(cts.Token);

                    return Task.CompletedTask;
                })
            .AddQueryType(d => d.Field("foo").Resolve(""))
            .Services.BuildServiceProvider()
            .GetRequiredService<RequestExecutorManager>();

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

        manager.EvictRequestExecutor();

        var executorAfterEviction = await manager.GetExecutorAsync();

        Assert.Same(initialExecutor, executorAfterEviction);

        warmupResetEvent.Set();
        executorEvictedResetEvent.Wait(cts.Token);
        var executorAfterWarmup = await manager.GetExecutorAsync();

        Assert.NotSame(initialExecutor, executorAfterWarmup);

        cts.Dispose();
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public async Task WarmupSchemaTasks_Are_Applied_Correct_Number_Of_Times(
        bool keepWarm, int expectedWarmups)
    {
        // arrange
        var warmups = 0;
        var executorEvictedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var manager = new ServiceCollection()
            .AddGraphQL()
            .InitializeOnStartup(
                keepWarm: keepWarm,
                warmup: (_, _) =>
                {
                    warmups++;
                    return Task.CompletedTask;
                })
            .AddQueryType(d => d.Field("foo").Resolve(""))
            .Services.BuildServiceProvider()
            .GetRequiredService<RequestExecutorManager>();

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

        manager.EvictRequestExecutor();
        executorEvictedResetEvent.Wait(cts.Token);

        var executorAfterEviction = await manager.GetExecutorAsync();

        Assert.NotSame(initialExecutor, executorAfterEviction);
        Assert.Equal(expectedWarmups, warmups);
    }

    [Fact]
    public async Task Calling_GetExecutorAsync_Multiple_Times_Only_Creates_One_Executor()
    {
        // arrange
        var manager = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Field("foo").Resolve("");
            })
            .Services.BuildServiceProvider()
            .GetRequiredService<RequestExecutorManager>();

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

        var services = new ServiceCollection();
        services
            .AddGraphQL("schema1")
            .AddQueryType(d =>
            {
                schema1CreationResetEvent.Wait(cts.Token);
                d.Field("foo").Resolve("");
            });
        services
            .AddGraphQL("schema2")
            .AddQueryType(d =>
            {
                d.Field("foo").Resolve("");
            });
        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<RequestExecutorManager>();

        // act
        var executor1Task = Task.Run(async () => await manager.GetExecutorAsync("schema1"), cts.Token);
        var executor2Task = Task.Run(async () => await manager.GetExecutorAsync("schema2"), cts.Token);

        // assert
        await executor2Task;

        schema1CreationResetEvent.Set();

        await executor1Task;

        cts.Dispose();
    }
}

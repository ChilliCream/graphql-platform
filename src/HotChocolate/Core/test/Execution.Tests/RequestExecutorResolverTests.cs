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
        var executorEvictedResetEvent = new AutoResetEvent(false);

        var services = new ServiceCollection();
        services
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IRequestExecutorResolver>();

        resolver.Events.Subscribe(new ExecutorResolverEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Evicted)
            {
                executorEvictedResetEvent.Set();
            }
        }));

        // act
        var firstExecutor = await resolver.GetRequestExecutorAsync();
        var firstOperationCache = firstExecutor.Services.GetCombinedServices()
            .GetRequiredService<IPreparedOperationCache>();

        resolver.EvictRequestExecutor();
        executorEvictedResetEvent.WaitOne();

        var secondExecutor = await resolver.GetRequestExecutorAsync();
        var secondOperationCache = secondExecutor.Services.GetCombinedServices()
            .GetRequiredService<IPreparedOperationCache>();

        // assert
        Assert.NotSame(secondOperationCache, firstOperationCache);
    }

    [Fact]
    public async Task Executor_Should_Only_Be_Switched_Once_It_Is_Warmed_Up()
    {
        // arrange
        var warmupResetEvent = new AutoResetEvent(true);
        var executorEvictedResetEvent = new AutoResetEvent(false);

        var services = new ServiceCollection();
        services
            .AddGraphQL()
            .InitializeOnStartup(
                keepWarm: true,
                warmup: (_, _) =>
                {
                    warmupResetEvent.WaitOne();

                    return Task.CompletedTask;
                })
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IRequestExecutorResolver>();

        resolver.Events.Subscribe(new ExecutorResolverEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Evicted)
            {
                executorEvictedResetEvent.Set();
            }
        }));

        // act
        // assert
        var initialExecutor = await resolver.GetRequestExecutorAsync();
        warmupResetEvent.Reset();

        resolver.EvictRequestExecutor();

        var executorAfterEviction = await resolver.GetRequestExecutorAsync();

        Assert.Same(initialExecutor, executorAfterEviction);

        warmupResetEvent.Set();
        executorEvictedResetEvent.WaitOne();
        var executorAfterWarmup = await resolver.GetRequestExecutorAsync();

        Assert.NotSame(initialExecutor, executorAfterWarmup);
    }

    [Fact]
    public async Task Executor_Resolution_Should_Be_Parallel()
    {
        // arrange
        var schema1CreationResetEvent = new AutoResetEvent(false);

        var services = new ServiceCollection();
        services
            .AddGraphQL("schema1")
            .AddQueryType(d =>
            {
                schema1CreationResetEvent.WaitOne();
                d.Field("foo").Resolve("");
            });
        services
            .AddGraphQL("schema2")
            .AddQueryType(d =>
            {
                d.Field("foo").Resolve("");
            });
        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IRequestExecutorResolver>();

        // act
        var executor1Task = Task.Run(async () => await resolver.GetRequestExecutorAsync("schema1"));
        var executor2Task = Task.Run(async () => await resolver.GetRequestExecutorAsync("schema2"));

        // assert
        await executor2Task;

        schema1CreationResetEvent.Set();

        await executor1Task;
    }

    private sealed class ExecutorResolverEventObserver(Action<RequestExecutorEvent> onEvent)
        : IObserver<RequestExecutorEvent>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(RequestExecutorEvent value)
            => onEvent(value);
    }
}

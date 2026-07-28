using HotChocolate.Execution.Caching;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class PreparedOperationCacheTests
{
    [Fact]
    public async Task Operation_Cache_Should_Have_Configured_Capacity()
    {
        // arrange
        const int cacheCapacity = 517;
        var services = new ServiceCollection();
        services
            .AddGraphQL()
            .ModifyOptions(o => o.PreparedOperationCacheSize = cacheCapacity)
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var operationCache = executor.Schema.Services.GetRequiredService<IPreparedOperationCache>();

        // assert
        Assert.Equal(cacheCapacity, operationCache.Capacity);
    }

    [Fact]
    public async Task Operation_Cache_Should_Be_Scoped_To_Executor()
    {
        // arrange
        var executorEvicted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationToken = TestContext.Current.CancellationToken;

        var manager = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve(""))
            .Services.BuildServiceProvider()
            .GetRequiredService<RequestExecutorManager>();

        manager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Evicted)
            {
                executorEvicted.TrySetResult();
            }
        }));

        // act
        var firstExecutor = await manager.GetExecutorAsync(cancellationToken: cancellationToken);
        var firstOperationCache = firstExecutor.Schema.Services
            .GetRequiredService<IPreparedOperationCache>();

        manager.EvictExecutor();
        await executorEvicted.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        var secondExecutor = await manager.GetExecutorAsync(cancellationToken: cancellationToken);
        var secondOperationCache = secondExecutor.Schema.Services
            .GetRequiredService<IPreparedOperationCache>();

        // assert
        Assert.NotSame(secondExecutor, firstExecutor);
        Assert.NotSame(secondOperationCache, firstOperationCache);
    }
}

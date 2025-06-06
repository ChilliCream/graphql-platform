using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class AutoUpdateRequestExecutorProxyTests
{
    [Fact]
    public async Task Ensure_Executor_Is_Cached()
    {
        // arrange
        var resolver =
            new ServiceCollection()
                .AddGraphQL()
                .AddStarWarsRepositories()
                .AddStarWarsTypes()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<RequestExecutorManager>();

        var innerProxy = new RequestExecutorProxy(resolver, resolver, ISchemaDefinition.DefaultName);

        // act
        var proxy = await AutoUpdateRequestExecutorProxy.CreateAsync(innerProxy);
        var a = proxy.InnerExecutor;
        var b = proxy.InnerExecutor;

        // assert
        Assert.Same(a, b);
    }

    [Fact]
    public async Task Ensure_Executor_Is_Correctly_Swapped_When_Evicted()
    {
        // arrange
        var executorUpdatedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var resolver =
            new ServiceCollection()
                .AddGraphQL()
                .AddStarWarsRepositories()
                .AddStarWarsTypes()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<RequestExecutorManager>();
        var evicted = false;
        var updated = false;

        var innerProxy = new RequestExecutorProxy(resolver, resolver, ISchemaDefinition.DefaultName);

        var proxy = await AutoUpdateRequestExecutorProxy.CreateAsync(innerProxy, cts.Token);
        innerProxy.ExecutorEvicted += (_, _) =>
        {
            evicted = true;
            executorUpdatedResetEvent.Set();
        };
        innerProxy.ExecutorUpdated += (_, _) => updated = true;

        // act
        var a = proxy.InnerExecutor;
        resolver.EvictRequestExecutor();
        executorUpdatedResetEvent.Wait(cts.Token);

        var b = proxy.InnerExecutor;

        // assert
        Assert.NotSame(a, b);
        Assert.True(evicted);
        Assert.True(updated);
    }

    [Fact]
    public async Task Ensure_Manual_Proxy_Is_Not_Null()
    {
        // arrange
        // act
        async Task Action() => await AutoUpdateRequestExecutorProxy.CreateAsync(null!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Action);
    }
}

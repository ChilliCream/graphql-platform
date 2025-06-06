using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class RequestExecutorProxyTests
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
                .GetRequiredService<IRequestExecutorProvider>();

        // act
        var proxy = new RequestExecutorProxy(resolver, ISchemaDefinition.DefaultName);
        var a = await proxy.GetRequestExecutorAsync(CancellationToken.None);
        var b = await proxy.GetRequestExecutorAsync(CancellationToken.None);

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
                .GetRequiredService<IRequestExecutorProvider>();
        var evicted = false;
        var updated = false;

        var proxy = new RequestExecutorProxy(resolver, ISchemaDefinition.DefaultName);
        proxy.ExecutorEvicted += (_, _) =>
        {
            evicted = true;
            executorUpdatedResetEvent.Set();
        };
        proxy.ExecutorUpdated += (_, _) => updated = true;

        // act
        var a = await proxy.GetRequestExecutorAsync(CancellationToken.None);
        resolver.EvictRequestExecutor();
        executorUpdatedResetEvent.Wait(cts.Token);
        var b = await proxy.GetRequestExecutorAsync(CancellationToken.None);

        // assert
        Assert.NotSame(a, b);
        Assert.True(evicted);
        Assert.True(updated);
    }
}

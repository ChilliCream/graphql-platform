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
                .GetRequiredService<RequestExecutorManager>();

        // act
        var proxy = new RequestExecutorProxy(resolver, resolver, ISchemaDefinition.DefaultName);
        var a = await proxy.GetExecutorAsync(CancellationToken.None);
        var b = await proxy.GetExecutorAsync(CancellationToken.None);

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

        var proxy = new TestProxy(resolver, resolver, ISchemaDefinition.DefaultName);
        proxy.ExecutorEvicted += () =>
        {
            evicted = true;
            executorUpdatedResetEvent.Set();
        };
        proxy.ExecutorUpdated += () => updated = true;

        // act
        var a = await proxy.GetExecutorAsync(CancellationToken.None);
        resolver.EvictRequestExecutor();
        executorUpdatedResetEvent.Wait(cts.Token);
        var b = await proxy.GetExecutorAsync(CancellationToken.None);

        // assert
        Assert.NotSame(a, b);
        Assert.True(evicted);
        Assert.True(updated);
    }

    private class TestProxy(
        IRequestExecutorProvider executorProvider,
        IRequestExecutorEvents executorEvents,
        string schemaName)
        : RequestExecutorProxy(executorProvider, executorEvents, schemaName)
    {
        public event Action? ExecutorEvicted;
        public event Action? ExecutorUpdated;

        protected override void OnRequestExecutorUpdated(IRequestExecutor? executor)
        {
            if (executor is null)
            {
                ExecutorEvicted?.Invoke();
            }
            else
            {
                ExecutorUpdated?.Invoke();
            }
        }
    }
}

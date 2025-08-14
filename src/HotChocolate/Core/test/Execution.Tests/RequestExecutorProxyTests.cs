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
        var updated = false;

        var proxy = new TestProxy(resolver, resolver, ISchemaDefinition.DefaultName);
        proxy.ExecutorUpdated += () =>
        {
            updated = true;
            executorUpdatedResetEvent.Set();
        };

        // act
        var a = await proxy.GetExecutorAsync(CancellationToken.None);
        resolver.EvictRequestExecutor();
        executorUpdatedResetEvent.Wait(cts.Token);
        var b = await proxy.GetExecutorAsync(CancellationToken.None);

        // assert
        Assert.NotSame(a, b);
        Assert.True(updated);
    }

    private class TestProxy(
        IRequestExecutorProvider executorProvider,
        IRequestExecutorEvents executorEvents,
        string schemaName)
        : RequestExecutorProxy(executorProvider, executorEvents, schemaName)
    {
        public event Action? ExecutorUpdated;

        protected override void OnAfterRequestExecutorSwapped(
            IRequestExecutor newExecutor,
            IRequestExecutor oldExecutor)
        {
            ExecutorUpdated?.Invoke();
        }
    }
}

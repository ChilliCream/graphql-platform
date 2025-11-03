using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class DocumentCacheTests
{
    [Fact]
    public async Task Document_Cache_Should_Have_Configured_Capacity()
    {
        // arrange
        const int cacheCapacity = 517;
        var services = new ServiceCollection();
        services
            .AddGraphQL()
            .ModifyOptions(o => o.OperationDocumentCacheSize = cacheCapacity)
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync();

        // act
        var documentCache = executor.Schema.Services.GetRequiredService<IDocumentCache>();

        // assert
        Assert.Equal(cacheCapacity, documentCache.Capacity);
    }

    [Fact]
    public async Task Document_Cache_Should_Not_Be_Scoped_To_Executor()
    {
        // arrange
        var executorEvictedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var services =
            new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Field("foo").Resolve(""))
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<RequestExecutorManager>();

        manager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Evicted)
            {
                executorEvictedResetEvent.Set();
            }
        }));

        // act
        var firstExecutor = await manager.GetExecutorAsync(cancellationToken: cts.Token);
        var firstDocumentCache = firstExecutor.Schema.Services.GetRequiredService<IDocumentCache>();

        await firstExecutor.ExecuteAsync("{ __typename }", cts.Token);

        manager.EvictExecutor();
        executorEvictedResetEvent.Wait(cts.Token);

        var secondExecutor = await manager.GetExecutorAsync(cancellationToken: cts.Token);
        var secondDocumentCache = secondExecutor.Schema.Services.GetRequiredService<IDocumentCache>();

        // assert
        Assert.NotSame(secondExecutor, firstExecutor);
        Assert.Same(secondDocumentCache, firstDocumentCache);
        Assert.Equal(1, secondDocumentCache.Count);
    }

    [Fact]
    public async Task Document_Cache_Should_Be_Scoped_To_Schema()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var services = new ServiceCollection();
        services
            .AddGraphQL("a")
            .AddQueryType(d => d.Field("foo").Resolve(""));
        services
            .AddGraphQL("b")
            .AddQueryType(d => d.Field("foo").Resolve(""));

        var manager = services.BuildServiceProvider().GetRequiredService<RequestExecutorManager>();

        // act
        var executorA = await manager.GetExecutorAsync("a", cts.Token);
        var documentCacheA = executorA.Schema.Services.GetRequiredService<IDocumentCache>();

        var executorB = await manager.GetExecutorAsync("b", cts.Token);
        var documentCacheB = executorB.Schema.Services.GetRequiredService<IDocumentCache>();

        // assert
        Assert.NotSame(executorB, executorA);
        Assert.NotSame(documentCacheB, documentCacheA);
    }
}

using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class FusionDocumentCacheTests : FusionTestBase
{
    [Fact]
    public async Task Document_Cache_Should_Have_Configured_Capacity()
    {
        // arrange
        const int cacheCapacity = 517;
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .ModifyOptions(o => o.OperationDocumentCacheSize = cacheCapacity)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      field: String!
                    }
                    """));
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

        var configProvider = new TestFusionConfigurationProvider(
            CreateFusionConfiguration(
                """
                type Query {
                  field1: String!
                }
                """));

        var services =
            new ServiceCollection()
                .AddHttpClient()
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
        var firstDocumentCache = firstExecutor.Schema.Services.GetRequiredService<IDocumentCache>();

        await firstExecutor.ExecuteAsync("{ __typename }", cts.Token);

        configProvider.UpdateConfiguration(
            CreateFusionConfiguration(
                """
                type Query {
                  field2: String!
                }
                """));
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

        var services = new ServiceCollection().AddHttpClient();
        services
            .AddGraphQLGateway("a")
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      fieldA: String!
                    }
                    """));
        services
            .AddGraphQLGateway("b")
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      fieldB: String!
                    }
                    """));

        var manager = services.BuildServiceProvider().GetRequiredService<FusionRequestExecutorManager>();

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

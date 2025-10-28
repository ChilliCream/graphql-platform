using HotChocolate.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class OperationPlanCacheTests : FusionTestBase
{
    [Fact]
    public async Task Plan_Cache_Should_Have_Configured_Capacity()
    {
        // arrange
        const int cacheCapacity = 517;
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .ModifyOptions(o => o.OperationExecutionPlanCacheSize = cacheCapacity)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      field: String!
                    }
                    """));
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync();

        // act
        var operationPlanCache = executor.Schema.Services.GetRequiredService<Cache<OperationPlan>>();

        // assert
        Assert.Equal(cacheCapacity, operationPlanCache.Capacity);
    }

    [Fact]
    public async Task Plan_Cache_Should_Be_Scoped_To_Executor()
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
        var firstPlanCache = firstExecutor.Schema.Services
            .GetRequiredService<Cache<OperationPlan>>();

        configProvider.UpdateConfiguration(
            CreateFusionConfiguration(
                """
                type Query {
                  field2: String!
                }
                """));
        executorEvictedResetEvent.Wait(cts.Token);

        var secondExecutor = await manager.GetExecutorAsync(cancellationToken: cts.Token);
        var secondPlanCache = secondExecutor.Schema.Services
            .GetRequiredService<Cache<OperationPlan>>();

        // assert
        Assert.NotSame(secondExecutor, firstExecutor);
        Assert.NotSame(secondPlanCache, firstPlanCache);
    }
}

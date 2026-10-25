using HotChocolate.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

public class OperationPlanCacheMiddlewareTests : FusionTestBase
{
    [Fact]
    public async Task InvokeAsync_Should_UseCachedPlan_When_OperationWasPlannedBefore()
    {
        // arrange
        var cacheDiagnostics = new CountingCacheDiagnostics();
        var listener = new PlanCacheListener();
        var planSetOnEntry = new List<bool>();
        var executor = await new ServiceCollection()
            .AddHttpClient()
            .AddGraphQLGateway()
            .ModifyOptions(o => o.OperationExecutionPlanCacheDiagnostics = cacheDiagnostics)
            .AddDiagnosticEventListener(_ => listener)
            .UseRequest(
                (_, next) => context =>
                {
                    planSetOnEntry.Add(context.GetOperationPlan() is not null);
                    return next(context);
                },
                after: WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                allowMultiple: true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      field: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await executor.ExecuteAsync("{ __typename }", TestContext.Current.CancellationToken);
        var result = await executor.ExecuteAsync("{ __typename }", TestContext.Current.CancellationToken);

        // assert
        Snapshot.Create()
            .Add(result, "Result")
            .Add(
                new
                {
                    CacheHits = cacheDiagnostics.Hits,
                    CacheMisses = cacheDiagnostics.Misses,
                    listener.AddedToCache,
                    listener.RetrievedFromCache,
                    PlanSetOnEntry = planSetOnEntry
                },
                "Plan Cache")
            .MatchMarkdownSnapshot();
    }

    private sealed class CountingCacheDiagnostics : CacheDiagnostics
    {
        private int _hits;
        private int _misses;

        public int Hits => _hits;

        public int Misses => _misses;

        public override void RegisterSizeGauge(Func<long> sizeProvider)
        {
        }

        public override void RegisterCapacityGauge(Func<long> sizeProvider)
        {
        }

        public override void Hit() => Interlocked.Increment(ref _hits);

        public override void Miss() => Interlocked.Increment(ref _misses);

        public override void Evict()
        {
        }
    }

    private sealed class PlanCacheListener : FusionExecutionDiagnosticEventListener
    {
        private int _addedToCache;
        private int _retrievedFromCache;

        public int AddedToCache => _addedToCache;

        public int RetrievedFromCache => _retrievedFromCache;

        public override void AddedOperationPlanToCache(RequestContext context, string operationPlanId)
            => Interlocked.Increment(ref _addedToCache);

        public override void RetrievedOperationPlanFromCache(RequestContext context, string operationPlanId)
            => Interlocked.Increment(ref _retrievedFromCache);
    }
}

using System.Collections.Immutable;
using HotChocolate.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class OperationPlanCacheTests : FusionTestBase
{
    [Fact]
    public void Add_Should_NotDriftIndexCount_When_PlanRepeatsPolicyName()
    {
        // arrange
        // The same policy name reached through two condition slots (for example the same policy
        // applied at two roots with a different residual denial threshold) mirrors what
        // OperationPlan.CreatePolicyPlanEntries legitimately emits as two distinct entries that
        // share one PolicyName.
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                """
                type Query {
                  field: String!
                }
                """));
        var basePlan = PlanOperation(schema, "{ field }");
        var groups = ImmutableArray.Create(ImmutableArray.Create("Shared"));
        var policySlots = ImmutableArray.Create(
            new PolicyConditionSlot { Ordinal = 0, Groups = groups, Rmax = PolicyDenialBehavior.Null },
            new PolicyConditionSlot { Ordinal = 1, Groups = groups, Rmax = PolicyDenialBehavior.Error });
        var plan = OperationPlan.Create(
            "plan-with-duplicate-policy-name",
            basePlan.Operation,
            basePlan.RootNodes,
            basePlan.AllNodes,
            basePlan.DeliveryGroups,
            basePlan.IncrementalPlans,
            policySlots,
            basePlan.SearchSpace,
            basePlan.ExpandedNodes);
        Assert.Equal(2, plan.Policies.Count(p => p.PolicyName == "Shared"));

        var planCache = new OperationPlanCache(16, diagnostics: null);
        var session = planCache.Capture();

        // act
        // Add the same id twice: once with the duplicate PolicyName entry, once more to also
        // exercise a re-Add of an id that is already indexed.
        planCache.Add(session, "plan", plan);
        planCache.Add(session, "plan", plan);

        // assert
        Assert.Equal(1, planCache.IndexedIdCountForTesting);
    }

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
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

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

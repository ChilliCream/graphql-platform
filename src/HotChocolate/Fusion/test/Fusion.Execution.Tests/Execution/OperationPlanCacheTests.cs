using System.Collections.Immutable;
using HotChocolate.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class OperationPlanCacheTests : FusionTestBase
{
    [Fact]
    public async Task CreatePlan_Should_InventoryRequirementFreePolicyNames_When_TargetContainsGroups()
    {
        // arrange
        await using var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(
                _ => new TestPolicyProvider(new TestPolicy("CanRead"), new TestPolicy("CanAudit")))
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                """
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  field: String @policy(names: [["CanRead", "CanAudit"]])
                }
                """),
            services);

        // act
        var plan = PlanOperation(schema, "{ field }");

        // assert
        Assert.Collection(
            plan.Policies.OrderBy(entry => entry.PolicyName, StringComparer.Ordinal),
            entry => Assert.Equal(
                ("CanAudit", PolicyPlanEntry.ComputeRequirementHash(null)),
                (entry.PolicyName, entry.RequirementHash)),
            entry => Assert.Equal(
                ("CanRead", PolicyPlanEntry.ComputeRequirementHash(null)),
                (entry.PolicyName, entry.RequirementHash)));
    }

    [Fact]
    public async Task CreatePlan_Should_InventoryPolicies_When_PlanContainsSlotAndTarget()
    {
        // arrange
        await using var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(
                _ => new TestPolicyProvider(
                    new TestPolicy("CanRequest"),
                    new TestPolicy(
                        "CanResource",
                        Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }"))))
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                """
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  id: ID!
                  field: String!
                    @policy(names: [["CanRequest"]])
                    @policy(names: [["CanResource"]])
                }
                """),
            services);

        // act
        var plan = PlanOperation(schema, "{ field }");

        // assert
        Assert.Single(plan.PolicySlots);
        Assert.Single(plan.AllNodes.OfType<PolicyExecutionNode>());
        Assert.Collection(
            plan.Policies,
            entry => Assert.Equal(
                ("CanRequest", PolicyPlanEntry.ComputeRequirementHash(null)),
                (entry.PolicyName, entry.RequirementHash)),
            entry => Assert.Equal(
                ("CanResource", PolicyPlanEntry.ComputeRequirementHash(
                    Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }"))),
                (entry.PolicyName, entry.RequirementHash)));
    }

    [Fact]
    public async Task CreatePlan_Should_UseResourceRequirementHash_When_ApplicationMixesPolicyKinds()
    {
        // arrange
        await using var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(
                _ => new TestPolicyProvider(
                    new TestPolicy("CanRequest"),
                    new TestPolicy(
                        "CanResource",
                        Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }"))))
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                """
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query @policy(names: [["CanRequest", "CanResource"]]) {
                  id: ID!
                  field: String!
                }
                """),
            services);

        // act
        var plan = PlanOperation(schema, "{ field }");

        // assert
        Assert.Collection(
            plan.Policies.OrderBy(entry => entry.PolicyName, StringComparer.Ordinal),
            entry => Assert.Equal(
                ("CanRequest", PolicyPlanEntry.ComputeRequirementHash(null)),
                (entry.PolicyName, entry.RequirementHash)),
            entry =>
            {
                Assert.Equal("CanResource", entry.PolicyName);
                Assert.NotEqual(0UL, entry.RequirementHash);
            });
    }

    [Fact]
    public async Task CreatePlan_Should_RejectMultipleRequirementHashesForSamePolicyName()
    {
        // arrange
        var plan = await CreateDataBearingPolicyPlanAsync("Shared");
        var secondRequirement = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ name }");
        var policies = plan.Policies.Add(
            new PolicyPlanEntry
            {
                PolicyName = "Shared",
                RequirementHash = PolicyPlanEntry.ComputeRequirementHash(secondRequirement)
            })
            .OrderBy(entry => entry.PolicyName, StringComparer.Ordinal)
            .ThenBy(entry => entry.RequirementHash)
            .ToImmutableArray();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => OperationPlan.Create(
                "multiple-policy-requirements",
                plan.Operation,
                plan.RootNodes,
                plan.AllNodes,
                plan.DeliveryGroups,
                plan.IncrementalPlans,
                plan.IncludeConditions,
                plan.PolicyExpressions,
                plan.PolicySlots,
                policies,
                plan.SearchSpace,
                plan.ExpandedNodes));

        // assert
        Assert.Equal(
            "The policy inventory does not match the compiled policy occurrences.",
            exception.Message);
    }

    [Fact]
    public async Task CreatePlan_Should_RejectEmptyInventory_When_PlanContainsOnlyPolicyTarget()
    {
        // arrange
        var plan = await CreateDataBearingPolicyPlanAsync("Shared");

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => OperationPlan.Create(
                "missing-target-inventory",
                plan.Operation,
                plan.RootNodes,
                plan.AllNodes,
                plan.DeliveryGroups,
                plan.IncrementalPlans,
                plan.IncludeConditions,
                plan.PolicyExpressions,
                plan.PolicySlots,
                [],
                plan.SearchSpace,
                plan.ExpandedNodes));

        // assert
        Assert.Equal(
            "The policy inventory does not match the compiled policy occurrences.",
            exception.Message);
    }

    [Fact]
    public async Task CreatePlan_Should_RejectDuplicateCompiledPolicyTarget()
    {
        // arrange
        var plan = await CreateDataBearingPolicyPlanAsync("Shared");
        var policyNode = Assert.Single(plan.AllNodes.OfType<PolicyExecutionNode>());
        var target = Assert.Single(policyNode.Targets.ToArray());
        var duplicateNode = new PolicyExecutionNode(
            policyNode.Id,
            [target, target],
            policyNode.Conditions.ToArray());
        var allNodes = plan.AllNodes
            .Select(node => node.Id == policyNode.Id ? duplicateNode : node)
            .ToImmutableArray();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => OperationPlan.Create(
                "duplicate-policy-target",
                plan.Operation,
                plan.RootNodes,
                allNodes,
                plan.DeliveryGroups,
                plan.IncrementalPlans,
                plan.IncludeConditions,
                plan.PolicyExpressions,
                plan.PolicySlots,
                plan.Policies,
                plan.SearchSpace,
                plan.ExpandedNodes));

        // assert
        Assert.Equal(
            "A residual policy target has no unique compiled occurrence.",
            exception.Message);
    }

    [Fact]
    public async Task Add_Should_NotDriftIndexCount_When_PlanHasPolicyInMultipleSlots()
    {
        // arrange
        var plan = await CreatePlanWithPolicyInMultipleSlotsAsync("Shared");
        var planCache = new OperationPlanCache(16, diagnostics: null);
        var session = planCache.Capture();

        // act
        planCache.Add(session, "plan", plan);
        planCache.Add(session, "plan", plan);

        // assert
        Assert.Equal(1, planCache.IndexedIdCountForTesting);
    }

    [Fact]
    public async Task Add_Should_DiscardStaleSessionIndex_When_CacheIsReset()
    {
        // arrange
        var planCache = new OperationPlanCache(16, diagnostics: null);
        var staleSession = planCache.Capture();
        var plan = await CreatePlanWithPolicyInMultipleSlotsAsync("Shared");

        // act
        planCache.Reset();
        planCache.Add(staleSession, "stale", plan);
        var cached = planCache.Current.TryGet("stale", out _);

        // assert
        Assert.Equal((Cached: false, IndexedIds: 0L), (Cached: cached, IndexedIds: planCache.IndexedIdCountForTesting));
    }

    [Fact]
    public async Task Add_Should_NotPublishCapturedPlan_When_TargetedEvictionOccursBeforePublication()
    {
        // arrange
        var planCache = new OperationPlanCache(16, diagnostics: null);
        var session = planCache.Capture();
        var plan = await CreatePlanWithPolicyInMultipleSlotsAsync("Shared");
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        planCache.BeforeAddValidation = () =>
        {
            entered.Set();
            release.Wait(TestContext.Current.CancellationToken);
        };

        // act
        var add = Task.Run(
            () => planCache.Add(session, "stale", plan),
            TestContext.Current.CancellationToken);
        entered.Wait(TestContext.Current.CancellationToken);
        planCache.EvictPolicies(new Dictionary<string, ulong>(StringComparer.Ordinal)
        {
            ["Shared"] = 1
        });
        release.Set();
        await add;

        // assert
        Assert.Equal(
            (Cached: false, IndexedIds: 0L),
            (
                Cached: planCache.Current.TryGet("stale", out _),
                IndexedIds: planCache.IndexedIdCountForTesting));
    }

    [Fact]
    public async Task Add_Should_KeepCurrentGenerationIndex_When_ResetRacesOldSessionPublication()
    {
        // arrange
        var planCache = new OperationPlanCache(16, diagnostics: null);
        var oldSession = planCache.Capture();
        var plan = await CreatePlanWithPolicyInMultipleSlotsAsync("Shared");
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        planCache.BeforeAddValidation = () =>
        {
            entered.Set();
            release.Wait(TestContext.Current.CancellationToken);
        };

        // act
        var oldAdd = Task.Run(
            () => planCache.Add(oldSession, "old", plan),
            TestContext.Current.CancellationToken);
        entered.Wait(TestContext.Current.CancellationToken);
        planCache.Reset();
        planCache.BeforeAddValidation = null;
        planCache.Add(planCache.Capture(), "current", plan);
        release.Set();
        await oldAdd;

        // assert
        Assert.Equal(
            (OldCached: false, CurrentCached: true, IndexedIds: 1L),
            (
                OldCached: planCache.Current.TryGet("old", out _),
                CurrentCached: planCache.Current.TryGet("current", out _),
                IndexedIds: planCache.IndexedIdCountForTesting));
    }

    [Fact]
    public async Task Add_Should_SweepEvictedPlanIds_When_IndexExceedsMaintenanceThreshold()
    {
        // arrange
        var planCache = new OperationPlanCache(1, diagnostics: null);
        var session = planCache.Capture();
        var plan = await CreatePlanWithPolicyInMultipleSlotsAsync("Shared");

        // act
        for (var i = 0; i < 5; i++)
        {
            planCache.Add(session, $"plan-{i}", plan);
        }

        // assert
        Assert.Equal((long)planCache.Current.Count, planCache.IndexedIdCountForTesting);
    }

    private static async Task<OperationPlan> CreatePlanWithPolicyInMultipleSlotsAsync(
        string policyName)
    {
        await using var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(_ => new TestPolicyProvider(new TestPolicy(policyName)))
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                $$"""
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  first: String @policy(names: "{{policyName}}", onDenied: NULL)
                  second: String @policy(names: "{{policyName}}", onDenied: ERROR)
                }
                """),
            services);
        return PlanOperation(schema, "{ first second }");
    }

    private static async Task<OperationPlan> CreateDataBearingPolicyPlanAsync(string policyName)
    {
        await using var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(
                _ => new TestPolicyProvider(
                    new TestPolicy(
                        policyName,
                        Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }"))))
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                $$"""
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  id: ID!
                  field: String @policy(names: "{{policyName}}")
                }
                """),
            services);
        return PlanOperation(schema, "{ field }");
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

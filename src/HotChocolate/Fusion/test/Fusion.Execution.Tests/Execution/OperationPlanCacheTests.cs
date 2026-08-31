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
    public void CreatePlan_Should_InventoryRequirementFreePolicyNames_When_TargetContainsGroups()
    {
        // arrange
        using var services = new ServiceCollection()
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
            entry => Assert.Equal(("CanAudit", 0UL), (entry.PolicyName, entry.RequirementHash)),
            entry => Assert.Equal(("CanRead", 0UL), (entry.PolicyName, entry.RequirementHash)));
    }

    [Fact]
    public void CreatePlan_Should_InventoryPolicyNameOnce_When_NameOccursInSlotsAndTarget()
    {
        // arrange
        using var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(_ => new TestPolicyProvider(new TestPolicy("Shared")))
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                """
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query @policy(names: "Shared") {
                  field: String!
                }
                """),
            services);

        // act
        var plan = PlanOperation(schema, "{ field }");

        // assert
        Assert.Contains(
            plan.PolicySlots,
            slot => slot.Groups.Any(group => group.Contains("Shared", StringComparer.Ordinal)));
        Assert.Contains(
            plan.AllNodes
                .OfType<PolicyExecutionNode>()
                .SelectMany(node => node.Targets.ToArray())
                .SelectMany(target => target.Policies)
                .SelectMany(application => application.Groups)
                .SelectMany(group => group),
            name => name == "Shared");
        Assert.Single(plan.Policies, entry => entry.PolicyName == "Shared");
    }

    [Fact]
    public void CreatePlan_Should_UseResourceRequirementHash_When_ApplicationMixesPolicyKinds()
    {
        // arrange
        using var services = new ServiceCollection()
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
            entry => Assert.Equal(("CanRequest", 0UL), (entry.PolicyName, entry.RequirementHash)),
            entry =>
            {
                Assert.Equal("CanResource", entry.PolicyName);
                Assert.NotEqual(0UL, entry.RequirementHash);
            });
    }

    [Fact]
    public void CreatePlan_Should_KeepDistinctRequirementHashes_When_PolicyNameHasMultipleRequirements()
    {
        // arrange
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument("type Query { field: String! }"));
        var basePlan = PlanOperation(schema, "{ field }");
        var firstRequirement = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }");
        var secondRequirement = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ name }");
        var target = new PolicyExecutionTarget
        {
            Kind = PolicyTargetKind.Object,
            Path = SelectionPath.Root,
            TypeName = "Query",
            Policies = [CreatePolicyApplication("Shared")],
            Requirements =
            [
                new PolicyRequirement { PolicyName = "Shared", SelectionSet = firstRequirement },
                new PolicyRequirement { PolicyName = "Shared", SelectionSet = secondRequirement }
            ]
        };
        var node = new PolicyExecutionNode(1, [target], []);
        var plan = OperationPlan.Create(
            "multiple-policy-requirements",
            basePlan.Operation,
            [node],
            [node],
            [],
            [],
            [],
            basePlan.SearchSpace,
            basePlan.ExpandedNodes);
        var cache = new OperationPlanCache(16, diagnostics: null);

        // act
        cache.Add(cache.Capture(), "plan", plan);
        cache.EvictPolicies(new Dictionary<string, ulong>(StringComparer.Ordinal)
        {
            ["Shared"] = PolicyPlanEntry.ComputeRequirementHash(firstRequirement)
        });

        // assert
        Assert.Equal(
            new[]
            {
                PolicyPlanEntry.ComputeRequirementHash(firstRequirement),
                PolicyPlanEntry.ComputeRequirementHash(secondRequirement)
            }.OrderBy(hash => hash),
            plan.Policies.Select(entry => entry.RequirementHash));
        Assert.False(cache.Current.TryGet("plan", out _));
    }

    [Fact]
    public void Add_Should_KeepPlan_When_DuplicateTargetRequirementsAgree()
    {
        // arrange
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument("type Query { field: String! }"));
        var basePlan = PlanOperation(schema, "{ field }");
        var requirement = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }");
        var targets = Enumerable.Range(0, 2)
            .Select(
                _ => new PolicyExecutionTarget
                {
                    Kind = PolicyTargetKind.Object,
                    Path = SelectionPath.Root,
                    TypeName = "Query",
                    Policies = [CreatePolicyApplication("Shared")],
                    Requirements =
                    [
                        new PolicyRequirement
                        {
                            PolicyName = "Shared",
                            SelectionSet = requirement
                        }
                    ]
                })
            .ToImmutableArray();
        var node = new PolicyExecutionNode(1, targets, []);
        var plan = OperationPlan.Create(
            "agreeing-policy-requirements",
            basePlan.Operation,
            [node],
            [node],
            [],
            [],
            [],
            basePlan.SearchSpace,
            basePlan.ExpandedNodes);
        var cache = new OperationPlanCache(16, diagnostics: null);

        // act
        cache.Add(cache.Capture(), "plan", plan);
        cache.EvictPolicies(new Dictionary<string, ulong>(StringComparer.Ordinal)
        {
            ["Shared"] = PolicyPlanEntry.ComputeRequirementHash(requirement)
        });

        // assert
        Assert.True(cache.Current.TryGet("plan", out var cachedPlan));
        Assert.Same(plan, cachedPlan);
        Assert.Equal(1, cache.IndexedIdCountForTesting);
    }

    [Fact]
    public void Add_Should_NotDriftIndexCount_When_PlanHasPolicyInMultipleSlots()
    {
        // arrange
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                """
                type Query {
                  field: String!
                }
                """));
        var plan = CreatePlanWithPolicyInMultipleSlots(schema, "Shared");
        var planCache = new OperationPlanCache(16, diagnostics: null);
        var session = planCache.Capture();

        // act
        planCache.Add(session, "plan", plan);
        planCache.Add(session, "plan", plan);

        // assert
        Assert.Equal(1, planCache.IndexedIdCountForTesting);
    }

    [Fact]
    public void Add_Should_DiscardStaleSessionIndex_When_CacheIsReset()
    {
        // arrange
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                """
                type Query {
                  field: String!
                }
                """));
        var planCache = new OperationPlanCache(16, diagnostics: null);
        var staleSession = planCache.Capture();
        var plan = CreatePlanWithPolicyInMultipleSlots(schema, "Shared");

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
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument("type Query { field: String! }"));
        var planCache = new OperationPlanCache(16, diagnostics: null);
        var session = planCache.Capture();
        var plan = CreatePlanWithPolicyInMultipleSlots(schema, "Shared");
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        planCache.BeforeAddValidation = () =>
        {
            entered.Set();
            release.Wait(TestContext.Current.CancellationToken);
        };

        // act
        var add = Task.Run(() => planCache.Add(session, "stale", plan));
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
            (planCache.Current.TryGet("stale", out _), planCache.IndexedIdCountForTesting));
    }

    [Fact]
    public async Task Add_Should_KeepCurrentGenerationIndex_When_ResetRacesOldSessionPublication()
    {
        // arrange
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument("type Query { field: String! }"));
        var planCache = new OperationPlanCache(16, diagnostics: null);
        var oldSession = planCache.Capture();
        var plan = CreatePlanWithPolicyInMultipleSlots(schema, "Shared");
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        planCache.BeforeAddValidation = () =>
        {
            entered.Set();
            release.Wait(TestContext.Current.CancellationToken);
        };

        // act
        var oldAdd = Task.Run(() => planCache.Add(oldSession, "old", plan));
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
                planCache.Current.TryGet("old", out _),
                planCache.Current.TryGet("current", out _),
                planCache.IndexedIdCountForTesting));
    }

    [Fact]
    public void Add_Should_SweepEvictedPlanIds_When_IndexExceedsMaintenanceThreshold()
    {
        // arrange
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                """
                type Query {
                  field: String!
                }
                """));
        var planCache = new OperationPlanCache(1, diagnostics: null);
        var session = planCache.Capture();
        var plan = CreatePlanWithPolicyInMultipleSlots(schema, "Shared");

        // act
        for (var i = 0; i < 5; i++)
        {
            planCache.Add(session, $"plan-{i}", plan);
        }

        // assert
        Assert.Equal((long)planCache.Current.Count, planCache.IndexedIdCountForTesting);
    }

    private static OperationPlan CreatePlanWithPolicyInMultipleSlots(
        FusionSchemaDefinition schema,
        string policyName)
    {
        var basePlan = PlanOperation(schema, "{ field }");
        var groups = ImmutableArray.Create(ImmutableArray.Create(policyName));
        var policySlots = ImmutableArray.Create(
            new PolicyConditionSlot { Ordinal = 0, Groups = groups, Rmax = PolicyDenialBehavior.Null },
            new PolicyConditionSlot { Ordinal = 1, Groups = groups, Rmax = PolicyDenialBehavior.Error });
        return OperationPlan.Create(
            "plan-with-duplicate-policy-name",
            basePlan.Operation,
            basePlan.RootNodes,
            basePlan.AllNodes,
            basePlan.DeliveryGroups,
            basePlan.IncrementalPlans,
            policySlots,
            basePlan.SearchSpace,
            basePlan.ExpandedNodes);
    }

    private static PolicyApplication CreatePolicyApplication(string policyName)
        => new()
        {
            Groups = ImmutableArray.Create(ImmutableArray.Create(policyName)),
            OnDenied = PolicyDenialBehavior.Null
        };

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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Planning.Partitioners;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public class OperationPlannerCostModelTests : FusionTestBase
{
    [Fact]
    public void PathCost_Defaults_Prefer_ModerateFanout_To_SequentialChain()
    {
        // 4 sequential operations.
        var sequential = CreateNode(maxDepth: 4, operationStepCount: 4, excessFanout: 0);

        // 1 root + 6 parallel lookups.
        var moderateFanout = CreateNode(maxDepth: 2, operationStepCount: 7, excessFanout: 0);

        Assert.True(moderateFanout.PathCost < sequential.PathCost);
        Assert.Equal(66.0, sequential.PathCost, 6);
        Assert.Equal(40.5, moderateFanout.PathCost, 6);
    }

    [Fact]
    public void PathCost_Defaults_Penalize_ExcessiveFanout()
    {
        // 1 root + 8 parallel lookups (at threshold).
        var moderateFanout = CreateNode(maxDepth: 2, operationStepCount: 9, excessFanout: 0);

        // 1 root + 20 parallel lookups.
        var excessiveFanout = CreateNode(maxDepth: 2, operationStepCount: 21, excessFanout: 12);

        Assert.True(moderateFanout.PathCost < excessiveFanout.PathCost);
        Assert.Equal(43.5, moderateFanout.PathCost, 6);
        Assert.Equal(97.5, excessiveFanout.PathCost, 6);
    }

    [Fact]
    public void Constructors_Wire_Default_And_Custom_Options()
    {
        var schema = CreateCompositeSchema();
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var compiler = new OperationCompiler(schema, pool);

        var defaultPlanner = new OperationPlanner(schema, compiler);

        Assert.Equal(15.0, defaultPlanner.Options.DepthWeight);
        Assert.Equal(1.5, defaultPlanner.Options.OperationWeight);
        Assert.Equal(3.0, defaultPlanner.Options.ExcessFanoutWeight);
        Assert.Equal(8, defaultPlanner.Options.FanoutPenaltyThreshold);

        var customOptions = new OperationPlannerOptions
        {
            DepthWeight = 9.0,
            OperationWeight = 5.0,
            ExcessFanoutWeight = 2.0,
            FanoutPenaltyThreshold = 4
        };

        var customPlanner = new OperationPlanner(schema, compiler, customOptions);

        Assert.Equal(customOptions.DepthWeight, customPlanner.Options.DepthWeight);
        Assert.Equal(customOptions.OperationWeight, customPlanner.Options.OperationWeight);
        Assert.Equal(customOptions.ExcessFanoutWeight, customPlanner.Options.ExcessFanoutWeight);
        Assert.Equal(customOptions.FanoutPenaltyThreshold, customPlanner.Options.FanoutPenaltyThreshold);
    }

    [Fact]
    public void RemainingCost_Projects_RemainingDepth_For_EqualOperationFloor()
    {
        // Both states project the same operation floor (3 operations),
        // but one is a deeper remaining chain.
        var deepChain = CreateBacklogCost(3, 4, 5);
        var flatParallel = CreateBacklogCost(3, 3, 3);

        Assert.Equal(deepChain.MinimumOperationCount, flatParallel.MinimumOperationCount);

        var currentOpsPerLevel = ImmutableDictionary<int, int>.Empty.Add(2, 1);

        var deepChainCost = PlannerCostEstimator.EstimateRemainingCost(
            OperationPlannerOptions.Default,
            currentMaxDepth: 2,
            currentOpsPerLevel,
            deepChain);

        var flatParallelCost = PlannerCostEstimator.EstimateRemainingCost(
            OperationPlannerOptions.Default,
            currentMaxDepth: 2,
            currentOpsPerLevel,
            flatParallel);

        Assert.Equal(49.5, deepChainCost, 6);
        Assert.Equal(19.5, flatParallelCost, 6);
        Assert.True(deepChainCost > flatParallelCost);
    }

    [Fact]
    public void RemainingCost_Projects_ExcessFanout_For_EqualOperationFloor()
    {
        // Both states project 10 remaining operations and no additional depth.
        // Only fan-out shape differs.
        var moderateFanout = CreateBacklogCost(2, 2, 2, 2, 2, 3, 3, 3, 3, 3);
        var excessiveFanout = CreateBacklogCost(2, 2, 2, 2, 2, 2, 2, 2, 2, 2);

        Assert.Equal(moderateFanout.MinimumOperationCount, excessiveFanout.MinimumOperationCount);

        var moderateFanoutCost = PlannerCostEstimator.EstimateRemainingCost(
            OperationPlannerOptions.Default,
            currentMaxDepth: 3,
#if NET10_0_OR_GREATER
            [],
#else
            ImmutableDictionary<int, int>.Empty,
#endif
            moderateFanout);

        var excessiveFanoutCost = PlannerCostEstimator.EstimateRemainingCost(
            OperationPlannerOptions.Default,
            currentMaxDepth: 3,
#if NET10_0_OR_GREATER
            [],
#else
            ImmutableDictionary<int, int>.Empty,
#endif
            excessiveFanout);

        Assert.Equal(15.0, moderateFanoutCost, 6);
        Assert.Equal(21.0, excessiveFanoutCost, 6);
        Assert.True(excessiveFanoutCost > moderateFanoutCost);
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(10.0)]
    public void RemainingCost_Should_NotExceedCompletionCost_When_OneOperationRemains(
        double operationWeight)
    {
        // arrange
        var options = new OperationPlannerOptions { OperationWeight = operationWeight };
        var backlogCost = CreateBacklogCost(1);
        var completed = CreateNode(1, 1, 0, options);

        // act
        var remainingCost = PlannerCostEstimator.EstimateRemainingCost(
            options,
            currentMaxDepth: 0,
#if NET10_0_OR_GREATER
            [],
#else
            ImmutableDictionary<int, int>.Empty,
#endif
            backlogCost);

        // assert
        Assert.True(
            remainingCost <= completed.PathCost,
            $"Remaining cost {remainingCost} exceeds completion cost {completed.PathCost}.");
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(10.0)]
    public void CreatePlan_Should_ChooseTwoRootFetches_When_GreedyCoverNeedsThree(
        double operationWeight)
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
                f1: Int @shareable
                f2: Int @shareable
                f3: Int @shareable
                f4: Int @shareable
            }
            """,
            """
            # name: b
            type Query {
                f1: Int @shareable
                f2: Int @shareable
                f5: Int
            }
            """,
            """
            # name: c
            type Query {
                f3: Int @shareable
                f4: Int @shareable
                f6: Int
            }
            """);
        var options = new OperationPlannerOptions { OperationWeight = operationWeight };

        // act
        var plan = PlanOperation(schema, "{ f1 f2 f3 f4 f5 f6 }", options);

        // assert
        plan.AllNodes
            .Cast<OperationExecutionNode>()
            .OrderBy(node => node.SchemaName, StringComparer.Ordinal)
            .Select(node => new
            {
                node.SchemaName,
                Fields = Utf8GraphQLParser.Parse(node.Operation.Value.Span)
                    .GetOperation(operationName: null)
                    .SelectionSet.Selections
                    .Cast<FieldNode>()
                    .Select(field => field.Name.Value)
                    .ToArray(),
                DependencyCount = node.Dependencies.Length
            })
            .ToArray()
            .MatchInlineSnapshot(
                """
                [
                  {
                    "SchemaName": "b",
                    "Fields": [
                      "f1",
                      "f2",
                      "f5"
                    ],
                    "DependencyCount": 0
                  },
                  {
                    "SchemaName": "c",
                    "Fields": [
                      "f3",
                      "f4",
                      "f6"
                    ],
                    "DependencyCount": 0
                  }
                ]
                """);
    }

    [Fact]
    public void CreatePlan_Should_ApplySchemaTieBreak_When_GreedyPlanHasEqualCost()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
                a: Int
            }
            """,
            """
            # name: b
            type Query {
                b: Int
                c: Int
            }
            """);
        var options = new OperationPlannerOptions { OperationWeight = 10.0 };

        // act
        var plan = PlanOperation(schema, "{ a b c }", options);

        // assert
        plan.AllNodes.Select(node => node.SchemaName).MatchInlineSnapshots(["a", "b"]);
    }

    [Fact]
    public void AddWorkItemCost_Should_CountOneOperation_When_WorkItemAlwaysAddsAnOperation()
    {
        // arrange
        var selectionSet = CreateSelectionSet();
        var workItems = new WorkItem[]
        {
            new OperationWorkItem(OperationWorkItemKind.Lookup, selectionSet, FromSchema: "test"),
            new OperationWorkItem(OperationWorkItemKind.Lookup, selectionSet, CreateLookup(), "test"),
            new OperationWorkItem(OperationWorkItemKind.Root, selectionSet),
            new NodeFieldWorkItem(
                new NodeField { Field = new FieldNode("node"), ParentFragments = null }),
            new NodeLookupWorkItem(null, "node", new HotChocolate.Language.IntValueNode(1), selectionSet)
        };

        // act
        var costs = workItems
            .Select(workItem => PlannerCostEstimator.AddWorkItemCost(BacklogCost.Empty, workItem))
            .Select(cost => cost.MinimumOperationCount)
            .ToArray();

        // assert
        costs.MatchInlineSnapshot(
            """
            [
              1,
              1,
              1,
              1,
              1
            ]
            """);
    }

    [Fact]
    public void AddWorkItemCost_Should_NotProjectDepth_When_PathLookupCanBecomeRootWorkItem()
    {
        // arrange
        var workItem = new OperationWorkItem(
            OperationWorkItemKind.Lookup,
            CreateSelectionSet(),
            FromSchema: "test")
        {
            ParentDepth = 3
        };

        // act
        var cost = PlannerCostEstimator.AddWorkItemCost(BacklogCost.Empty, workItem);

        // assert
        Assert.Equal(1, cost.MinimumOperationCount);
        Assert.Equal(0, cost.MaxProjectedDepth);
        Assert.Empty(cost.ProjectedOpsPerLevel);
    }

    [Fact]
    public void AddWorkItemCost_Should_ProjectDepth_When_LookupIsChosen()
    {
        // arrange
        var workItem = new OperationWorkItem(
            OperationWorkItemKind.Lookup,
            CreateSelectionSet(),
            CreateLookup(),
            "test")
        {
            ParentDepth = 3
        };

        // act
        var cost = PlannerCostEstimator.AddWorkItemCost(BacklogCost.Empty, workItem);

        // assert
        Assert.Equal(1, cost.MinimumOperationCount);
        Assert.Equal(4, cost.MaxProjectedDepth);
        Assert.Equal(1, cost.ProjectedOpsPerLevel[4]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddWorkItemCost_Should_NotCountOperationOrDepth_When_WorkItemIsRequirement(
        bool hasLookup)
    {
        // arrange
        var schema = CreateCompositeSchema();
        var field = schema.QueryType.Fields[0];
        var selection = new FieldSelection(1, new FieldNode(field.Name), field, SelectionPath.Root);
        var workItem = new FieldRequirementWorkItem(
            selection,
            new StepConsumer(1),
            hasLookup ? CreateLookup() : null)
        {
            ParentDepth = 3
        };

        // act
        var cost = PlannerCostEstimator.AddWorkItemCost(BacklogCost.Empty, workItem);

        // assert
        Assert.Equal(0, cost.MinimumOperationCount);
        Assert.Equal(0, cost.MaxProjectedDepth);
        Assert.Empty(cost.ProjectedOpsPerLevel);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-1.0)]
    public void Options_Should_Throw_When_WeightIsNotFiniteAndNonNegative(double weight)
    {
        // arrange
        var options = new OperationPlannerOptions();

        // act
        var errors = new[]
        {
            Assert.Throws<ArgumentException>(() => options.DepthWeight = weight).Message,
            Assert.Throws<ArgumentException>(() => options.OperationWeight = weight).Message,
            Assert.Throws<ArgumentException>(() => options.ExcessFanoutWeight = weight).Message
        };

        // assert
        errors.MatchInlineSnapshot(
            """
            [
              "The planner depth weight must be a finite, non-negative number.",
              "The planner operation weight must be a finite, non-negative number.",
              "The planner excess fan-out weight must be a finite, non-negative number."
            ]
            """);
    }

    [Fact]
    public void Options_Should_AcceptWeight_When_WeightIsZero()
    {
        // arrange
        var options = new OperationPlannerOptions();

        // act
        options.DepthWeight = 0;
        options.OperationWeight = 0;
        options.ExcessFanoutWeight = 0;

        // assert
        Assert.Equal(0.0, options.DepthWeight + options.OperationWeight + options.ExcessFanoutWeight);
    }

    [Fact]
    public void ComparePlansForTieBreak_Should_PreferFewerSurvivingSteps_When_CostsTie()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var plan1 = CreateTieNode(
            CreateStep(1, "a", "query Test { a }", schema, lookup: null, SelectionPath.Root, SelectionPath.Root, []),
            CreateStep(2, "z", "query Test { __typename @fusion__empty }", schema, lookup: null, SelectionPath.Root, SelectionPath.Root, []));
        var plan2 = CreateTieNode(
            CreateStep(1, "a", "query Test { a }", schema, lookup: null, SelectionPath.Root, SelectionPath.Root, []),
            CreateStep(2, "a", "query Test { b }", schema, lookup: null, SelectionPath.Root, SelectionPath.Root, []));
        var cached = -1;

        // act
        var comparison = OperationPlanner.ComparePlansForTieBreak(plan1, plan2, ref cached);
        cached = -1;
        var reverseComparison = OperationPlanner.ComparePlansForTieBreak(plan2, plan1, ref cached);

        // assert
        Assert.Equal(-1, Math.Sign(comparison));
        Assert.Equal(1, Math.Sign(reverseComparison));
    }

    [Fact]
    public void ComparePlansForTieBreak_Should_PreferFewerSurvivingSteps_When_KeyOnlyLookupIsRemoved()
    {
        // arrange
        var schema = ComposeProductSchema();
        var lookup = GetProductByIdLookup(schema);
        var plan1 = CreateTieNode(
            CreateStep(1, "a", "query Test { product { id } }", schema, null, SelectionPath.Root, SelectionPath.Root, [2]),
            CreateStep(
                2,
                "b",
                "query Test($x: ID!) { productById(id: $x) { id } }",
                schema,
                lookup,
                SelectionPath.Parse("$.product"),
                SelectionPath.Parse("$.productById"),
                []));
        var plan2 = CreateTieNode(
            CreateStep(1, "a", "query Test { product { id } }", schema, null, SelectionPath.Root, SelectionPath.Root, [2]),
            CreateStep(
                2,
                "b",
                "query Test($x: ID!) { productById(id: $x) { name } }",
                schema,
                lookup,
                SelectionPath.Parse("$.product"),
                SelectionPath.Parse("$.productById"),
                []));
        var cached = -1;

        // act
        var comparison = OperationPlanner.ComparePlansForTieBreak(plan1, plan2, ref cached);
        cached = -1;
        var reverseComparison = OperationPlanner.ComparePlansForTieBreak(plan2, plan1, ref cached);

        // assert
        Assert.Equal(-1, Math.Sign(comparison));
        Assert.Equal(1, Math.Sign(reverseComparison));
    }

    [Fact]
    public void ComparePlansForTieBreak_Should_PreferFewerSurvivingSteps_When_SkipGatedKeyOnlyLookupIsRemoved()
    {
        // arrange
        var schema = ComposeProductSchema();
        var lookup = GetProductByIdLookup(schema);
        var plan1 = CreateTieNode(
            CreateStep(1, "a", "query Test { product { id } }", schema, null, SelectionPath.Root, SelectionPath.Root, [2]),
            CreateStep(
                2,
                "b",
                "query Test($x: ID!, $s: Boolean!) { productById(id: $x) { id @skip(if: $s) } }",
                schema,
                lookup,
                SelectionPath.Parse("$.product"),
                SelectionPath.Parse("$.productById"),
                []));
        var plan2 = CreateTieNode(
            CreateStep(1, "a", "query Test { product { id } }", schema, null, SelectionPath.Root, SelectionPath.Root, [2]),
            CreateStep(
                2,
                "b",
                "query Test($x: ID!) { productById(id: $x) { name } }",
                schema,
                lookup,
                SelectionPath.Parse("$.product"),
                SelectionPath.Parse("$.productById"),
                []));
        var cached = -1;

        // act
        var comparison = OperationPlanner.ComparePlansForTieBreak(plan1, plan2, ref cached);
        cached = -1;
        var reverseComparison = OperationPlanner.ComparePlansForTieBreak(plan2, plan1, ref cached);

        // assert
        Assert.Equal(-1, Math.Sign(comparison));
        Assert.Equal(1, Math.Sign(reverseComparison));
    }

    private static FusionSchemaDefinition ComposeProductSchema()
    {
        return ComposeSchema(
            """
            # name: a
            type Query {
                product: Product
            }

            type Product {
                id: ID! @shareable
            }
            """,
            """
            # name: b
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
                id: ID! @shareable
                name: String
            }
            """);
    }

    private static Lookup GetProductByIdLookup(FusionSchemaDefinition schema)
    {
        return schema.Types.GetType<FusionObjectTypeDefinition>("Product").Sources["b"].Lookups[0];
    }

    private static PlanNode CreateTieNode(params PlanStep[] steps)
    {
        var node = CreateNode(maxDepth: 1, operationStepCount: steps.Length, excessFanout: 0);
        return node with { Steps = [.. steps] };
    }

    private static OperationPlanStep CreateStep(
        int id,
        string schemaName,
        [StringSyntax("graphql")] string operation,
        FusionSchemaDefinition schema,
        Lookup? lookup,
        SelectionPath target,
        SelectionPath source,
        ImmutableHashSet<int> dependents)
    {
        return new OperationPlanStep
        {
            Id = id,
            Definition = Utf8GraphQLParser
                .Parse(operation)
                .Definitions
                .OfType<OperationDefinitionNode>()
                .Single(),
            Type = schema.QueryType,
            RootSelectionSetId = 0,
            SelectionSets = [],
            SchemaName = schemaName,
            Target = target,
            Source = source,
            Lookup = lookup,
            Dependents = dependents
        };
    }

    private static PlanNode CreateNode(
        int maxDepth,
        int operationStepCount,
        int excessFanout,
        OperationPlannerOptions? options = null)
    {
        var operationDefinition = Utf8GraphQLParser
            .Parse("query Test { __typename }")
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();

        return new PlanNode
        {
            OperationDefinition = operationDefinition,
            InternalOperationDefinition = operationDefinition,
            ShortHash = "abcdef12",
            SchemaName = "None",
            Options = options ?? OperationPlannerOptions.Default,
            SelectionSetIndex = SelectionSetIndexer.Create(operationDefinition),
            Backlog = Backlog.Empty,
            RemainingCost = 0,
            OperationStepCount = operationStepCount,
            MaxDepth = maxDepth,
            ExcessFanout = excessFanout
        };
    }

    private BacklogCost CreateBacklogCost(params int[] projectedDepths)
    {
        var selectionSet = CreateSelectionSet();
        var lookup = CreateLookup();
        var backlogCost = BacklogCost.Empty;

        foreach (var depth in projectedDepths)
        {
            if (depth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(projectedDepths));
            }

            var workItem = new OperationWorkItem(
                OperationWorkItemKind.Lookup,
                selectionSet,
                lookup,
                FromSchema: "test")
            {
                ParentDepth = depth - 1
            };

            backlogCost = PlannerCostEstimator.AddWorkItemCost(backlogCost, workItem);
        }

        return backlogCost;
    }

    private SelectionSet CreateSelectionSet()
    {
        var schema = CreateCompositeSchema();
        var operationDefinition = Utf8GraphQLParser
            .Parse("query Test { __typename }")
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();

        return new SelectionSet(
            1,
            operationDefinition.SelectionSet,
            schema.QueryType,
            SelectionPath.Root);
    }

    private static Lookup CreateLookup()
        => new(
            "test",
            "Query",
            "lookup",
            "Query",
            isInternal: false,
            [new LookupArgument("id", new NamedTypeNode("ID"))],
            [new FieldSelectionMapParser("id").Parse()],
            []);
}

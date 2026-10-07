using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public sealed class OperationPlannerGuardrailTests : FusionTestBase
{
    [Fact]
    public void CreatePlan_Throws_When_MaxExpandedNodes_Guardrail_Is_Exceeded()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var planner = CreatePlanner(
            schema,
            new OperationPlannerOptions
            {
                MaxExpandedNodes = 1
            });
        var operation = ParseOperation(TestOperationText);

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan(
                "guardrail-expanded",
                "hash",
                "hash",
                operation,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(OperationPlannerGuardrailReason.MaxExpandedNodesExceeded, error.Reason);
        Assert.Equal(1, error.Limit);
        Assert.True(error.Observed > error.Limit);
    }

    [Fact]
    public void CreatePlan_Throws_When_MaxQueueSize_Guardrail_Is_Exceeded()
    {
        // arrange
        var schema = CreateMultiBranchSchema();
        var planner = CreatePlanner(
            schema,
            new OperationPlannerOptions
            {
                MaxQueueSize = 1
            });
        var operation = ParseOperation(MultiBranchOperationText);

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan(
                "guardrail-queue",
                "guardrail-queue",
                "12345678",
                operation,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(OperationPlannerGuardrailReason.MaxQueueSizeExceeded, error.Reason);
        Assert.Equal(1, error.Limit);
        Assert.True(error.Observed > error.Limit);
    }

    [Fact]
    public void CreatePlan_Throws_When_MaxPlanningTime_Guardrail_Is_Exceeded()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var planner = CreatePlanner(
            schema,
            new OperationPlannerOptions
            {
                MaxPlanningTime = TimeSpan.FromTicks(1)
            });
        var operation = ParseOperation(TestOperationText);

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan(
                "guardrail-time",
                "hash",
                "hash",
                operation,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(OperationPlannerGuardrailReason.MaxPlanningTimeExceeded, error.Reason);
        Assert.True(error.Observed >= error.Limit);
    }

    [Fact]
    public void CreatePlan_Throws_When_MaxGeneratedOptions_Guardrail_Is_Exceeded()
    {
        // arrange — use a 3-schema setup where a lookup work item produces
        // multiple candidate schemas, so expansion generates > 1 option.
        var schema = CreateMultiBranchSchema();
        var planner = CreatePlanner(
            schema,
            new OperationPlannerOptions
            {
                MaxGeneratedOptionsPerWorkItem = 1
            });
        var operation = ParseOperation(MultiBranchOperationText);

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan(
                "guardrail-generated",
                "guardrail-generated",
                "12345678",
                operation,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            OperationPlannerGuardrailReason.MaxGeneratedOptionsPerWorkItemExceeded,
            error.Reason);
        Assert.Equal(1, error.Limit);
        Assert.True(error.Observed > error.Limit);
    }

    [Fact]
    public void CreatePlan_Should_Throw_When_GreedyExpansionExceedsMaxExpandedNodes()
    {
        // arrange
        var planner = CreatePlanner(
            CreateSerialMutationSchema(),
            new OperationPlannerOptions { MaxExpandedNodes = 1 });
        var operation = ParseOperation(SerialMutationText);

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan(
                "guardrail-greedy-expanded",
                "hash",
                "12345678",
                operation,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(OperationPlannerGuardrailReason.MaxExpandedNodesExceeded, error.Reason);
        Assert.Equal(1, error.Limit);
        Assert.Equal(2, error.Observed);
    }

    [Fact]
    public void CreatePlan_Should_PlanSerialMutations_When_ExpansionBudgetIsSufficient()
    {
        // arrange
        var planner = CreatePlanner(
            CreateSerialMutationSchema(),
            new OperationPlannerOptions { MaxExpandedNodes = 32 });
        var operation = ParseOperation(SerialMutationText);

        // act
        var plan = planner.CreatePlan(
            "guardrail-greedy-expanded-control",
            "hash",
            "12345678",
            operation,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(3, plan.AllNodes.OfType<OperationExecutionNode>().Count());
    }

    [Fact]
    public void CreatePlan_Should_Throw_When_GreedyAndSearchExpansionsTogetherExceedMaxExpandedNodes()
    {
        // arrange
        // the limit equals the nodes the main search expands on its own, so only the greedy expansions can exceed it
        var schema = CreateSerialMutationSchema();
        var operation = ParseOperation(SerialMutationText);
        var searchExpandedNodes = CreatePlanner(schema, new OperationPlannerOptions())
            .CreatePlan(
                "guardrail-greedy-shared-baseline",
                "hash",
                "12345678",
                operation,
                TestContext.Current.CancellationToken)
            .ExpandedNodes;
        var planner = CreatePlanner(
            schema,
            new OperationPlannerOptions { MaxExpandedNodes = searchExpandedNodes });

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan(
                "guardrail-greedy-shared",
                "hash",
                "12345678",
                operation,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(OperationPlannerGuardrailReason.MaxExpandedNodesExceeded, error.Reason);
        Assert.Equal(searchExpandedNodes, error.Limit);
        Assert.Equal(searchExpandedNodes + 1, error.Observed);
    }

    [Fact]
    public void CreatePlan_Should_Throw_When_InitialQueueExceedsMaxQueueSizeByOne()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
            type Query { value: Int @shareable }
            """,
            """
            # name: b
            type Query { value: Int @shareable }
            """);
        var planner = CreatePlanner(schema, new OperationPlannerOptions { MaxQueueSize = 1 });
        var operation = ParseOperation("{ value }");

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan(
                "guardrail-initial-queue",
                "hash",
                "12345678",
                operation,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(OperationPlannerGuardrailReason.MaxQueueSizeExceeded, error.Reason);
        Assert.Equal(1, error.Limit);
        Assert.Equal(2, error.Observed);
    }

    [Fact]
    public void CreatePlan_Should_Throw_When_GreedyExpansionExceedsMaxGeneratedOptions()
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
        var planner = CreatePlanner(
            schema,
            new OperationPlannerOptions { MaxGeneratedOptionsPerWorkItem = 1 });
        var operation = ParseOperation("{ f1 f2 f3 f4 f5 f6 }");

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan(
                "guardrail-greedy-generated",
                "hash",
                "12345678",
                operation,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            OperationPlannerGuardrailReason.MaxGeneratedOptionsPerWorkItemExceeded,
            error.Reason);
        Assert.Equal(1, error.Limit);
        Assert.Equal(2, error.Observed);
    }

    [Fact]
    public void CreatePlan_Should_Throw_When_DeferredPlansTogetherExceedMaxExpandedNodes()
    {
        // arrange
        var planner = CreatePlanner(
            CreateDeferredRootSchema(),
            new OperationPlannerOptions { EnableDefer = true, MaxExpandedNodes = 4 });
        var operation = ParseOperation(DeferredRootOperationText);

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan(
                "guardrail-deferred-expanded",
                "hash",
                "12345678",
                operation,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(OperationPlannerGuardrailReason.MaxExpandedNodesExceeded, error.Reason);
        Assert.Equal(4, error.Limit);
        Assert.Equal(5, error.Observed);
    }

    [Fact]
    public void CreatePlan_Should_PlanDeferredGroups_When_ExpansionBudgetIsSufficient()
    {
        // arrange
        var planner = CreatePlanner(
            CreateDeferredRootSchema(),
            new OperationPlannerOptions { EnableDefer = true, MaxExpandedNodes = 32 });
        var operation = ParseOperation(DeferredRootOperationText);

        // act
        var plan = planner.CreatePlan(
            "guardrail-deferred-expanded-control",
            "hash",
            "12345678",
            operation,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(plan.AllNodes.OfType<OperationExecutionNode>());
        Assert.Equal(5, plan.IncrementalPlans.Length);
    }

    [Fact]
    public void CreatePlan_Should_Plan_When_MaxQueueSizeMatchesSearchPeak_And_GreedyRetainsSiblings()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product @key(fields: "id") {
              id: ID!
              region: String!
            }
            """,
            """
            schema {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              sku(region: String! @require(field: "region")): String! @shareable
            }
            """,
            """
            schema {
              query: Query
            }

            type Query {
              productBySku(sku: String!): Product @lookup @internal
            }

            type Product {
              sku: String!
              name: String!
            }
            """);
        var planner = CreatePlanner(schema, new OperationPlannerOptions { MaxQueueSize = 1 });
        var operation = ParseOperation("query GetTopProducts { topProducts { id name } }");

        // act
        var plan = planner.CreatePlan(
            "guardrail-greedy-queue-peak",
            "hash",
            "12345678",
            operation,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, plan.SearchSpace);
        Assert.Equal(3, plan.AllNodes.OfType<OperationExecutionNode>().Count());
    }

    [Fact]
    public void CreatePlan_Should_CountGreedyBacktrackingExpansions_When_MaxExpandedNodesIsSet()
    {
        // arrange
        var schema = ComposeSchema(
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external", "@requires"])

            type Query {
              root: T
            }

            type T @key(fields: "id") {
              id: ID!
              k: Int @external
              f: Int @requires(fields: "k")
            }
            """,
            GreedyKeyProvider,
            GreedyKeyProvider,
            GreedyKeyProvider,
            GreedyKeyProvider,
            GreedyKeyProvider,
            GreedyKeyProvider);
        var operation = ParseOperation("{ root { f } }");

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => CreatePlanner(schema, new OperationPlannerOptions { MaxExpandedNodes = 7 })
                .CreatePlan(
                    "guardrail-greedy-backtracking-exceeded",
                    "hash",
                    "12345678",
                    operation,
                    TestContext.Current.CancellationToken));
        var plan = CreatePlanner(schema, new OperationPlannerOptions { MaxExpandedNodes = 8 })
            .CreatePlan(
                "guardrail-greedy-backtracking-sufficient",
                "hash",
                "12345678",
                operation,
                TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(OperationPlannerGuardrailReason.MaxExpandedNodesExceeded, error.Reason);
        Assert.Equal(7, error.Limit);
        Assert.Equal(8, error.Observed);
        Assert.Equal(3, plan.ExpandedNodes);
    }

    private static FusionSchemaDefinition CreateSerialMutationSchema()
        => ComposeSchema(
            """
            # name: a
            type Query { a: Int }
            type Mutation { first: Int }
            """,
            """
            # name: b
            type Query { b: Int }
            type Mutation { second: Int }
            """,
            """
            # name: c
            type Query { c: Int }
            type Mutation { third: Int }
            """);

    private const string SerialMutationText = "mutation { first second third }";

    private static FusionSchemaDefinition CreateDeferredRootSchema()
        => ComposeSchema(
            """
            type Query {
                immediate: Int
                first: Int
                second: Int
                third: Int
                fourth: Int
                fifth: Int
            }
            """);

    private const string DeferredRootOperationText =
        """
        {
            immediate
            ... @defer(label: "first") { first }
            ... @defer(label: "second") { second }
            ... @defer(label: "third") { third }
            ... @defer(label: "fourth") { fourth }
            ... @defer(label: "fifth") { fifth }
        }
        """;

    private const string GreedyKeyProvider =
        """
        extend schema
          @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

        type T @key(fields: "id") {
          id: ID!
          k: Int @shareable
        }
        """;

    private static OperationPlanner CreatePlanner(
        FusionSchemaDefinition schema,
        OperationPlannerOptions options)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var compiler = new OperationCompiler(schema, pool);

        return new OperationPlanner(schema, compiler, options);
    }

    private static OperationDefinitionNode ParseOperation([StringSyntax("graphql")] string operationText)
        => Utf8GraphQLParser.Parse(operationText).Definitions.OfType<OperationDefinitionNode>().First();

    private const string TestOperationText =
        """
        {
          productBySlug(slug: "1") {
            id
            name
            estimatedDelivery(postCode: "12345")
          }
        }
        """;

    /// <summary>
    /// Composes a 3-schema setup where the root field is available in all schemas
    /// and a non-shared field forces lookup branching with multiple targets.
    /// This guarantees root expansion creates 3 branches and lookup expansion
    /// produces 2+ options per work item.
    /// </summary>
    private static FusionSchemaDefinition CreateMultiBranchSchema()
        => ComposeSchema(
            """
            schema { query: Query }
            type Query { itemById(id: ID!): Item @lookup @shareable }
            type Item @key(fields: "id") { id: ID! a: String! }
            """,
            """
            schema { query: Query }
            type Query { itemById(id: ID!): Item @lookup @shareable }
            type Item @key(fields: "id") { id: ID! b: String! @shareable }
            """,
            """
            schema { query: Query }
            type Query { itemById(id: ID!): Item @lookup @shareable }
            type Item @key(fields: "id") { id: ID! b: String! @shareable }
            """);

    private const string MultiBranchOperationText =
        """
        {
          itemById(id: "1") {
            a
            b
          }
        }
        """;
}

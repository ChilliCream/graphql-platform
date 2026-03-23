using CookieCrumble;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public class OperationMergePolicyTests : FusionTestBase
{
    [Fact]
    public void Aggressive_Merges_Cross_Depth_Operations()
    {
        // arrange — schema produces identical lookups to "d" at depth 1 and depth 3 (diff=2).
        var schema = CreateDeepCrossDepthSchema();

        // act
        var plan = PlanOperation(schema, DeepCrossDepthQuery, OperationMergePolicy.Aggressive);

        // assert — Aggressive merges regardless of depth difference.
        var batchOps = GetBatchOperationsForSchema(plan, "d");
        Assert.Contains(batchOps, op => op.Targets.Length > 1);
    }

    [Fact]
    public void Conservative_Does_Not_Merge_Cross_Depth_Operations()
    {
        // arrange
        var schema = CreateDeepCrossDepthSchema();

        // act
        var plan = PlanOperation(schema, DeepCrossDepthQuery, OperationMergePolicy.Conservative);

        // assert — Conservative only merges same-depth, so depth-1 vs depth-3
        // lookups must not share a batch.
        var batchOps = GetBatchOperationsForSchema(plan, "d");
        Assert.DoesNotContain(batchOps, op => op.Targets.Length > 1);
    }

    [Fact]
    public void Balanced_Does_Not_Merge_Distant_Depth_Operations()
    {
        // arrange — depth difference is 2, exceeding the Balanced threshold of 1.
        var schema = CreateDeepCrossDepthSchema();

        // act
        var plan = PlanOperation(schema, DeepCrossDepthQuery, OperationMergePolicy.Balanced);

        // assert — Balanced rejects merges when depth difference > 1.
        var batchOps = GetBatchOperationsForSchema(plan, "d");
        Assert.DoesNotContain(batchOps, op => op.Targets.Length > 1);
    }

    [Fact]
    public void Balanced_Merges_Adjacent_Depth_Operations()
    {
        // arrange — the simple cross-depth schema has depth diff = 1.
        var schema = CreateCrossDepthSchema();

        // act
        var plan = PlanOperation(schema, CrossDepthQuery, OperationMergePolicy.Balanced);

        // assert — Balanced allows merging when depth difference <= 1.
        var batchOps = GetBatchOperationsForSchema(plan, "c");
        Assert.Contains(batchOps, op => op.Targets.Length > 1);
    }

    [Fact]
    public void Aggressive_Same_Depth_Merges()
    {
        // arrange
        var schema = CreateSameDepthSchema();

        // act
        var plan = PlanOperation(schema, SameDepthQuery, OperationMergePolicy.Aggressive);

        // assert
        var batchOps = GetBatchOperationsForSchema(plan, "b");
        Assert.Single(batchOps);
        Assert.Equal(2, batchOps[0].Targets.Length);
    }

    [Fact]
    public void Conservative_Same_Depth_Merges()
    {
        // arrange
        var schema = CreateSameDepthSchema();

        // act
        var plan = PlanOperation(schema, SameDepthQuery, OperationMergePolicy.Conservative);

        // assert — even Conservative merges same-depth identical lookups.
        var batchOps = GetBatchOperationsForSchema(plan, "b");
        Assert.Single(batchOps);
        Assert.Equal(2, batchOps[0].Targets.Length);
    }

    [Fact]
    public void Balanced_Same_Depth_Merges()
    {
        // arrange
        var schema = CreateSameDepthSchema();

        // act
        var plan = PlanOperation(schema, SameDepthQuery, OperationMergePolicy.Balanced);

        // assert
        var batchOps = GetBatchOperationsForSchema(plan, "b");
        Assert.Single(batchOps);
        Assert.Equal(2, batchOps[0].Targets.Length);
    }

    [Fact]
    public void Cycle_Safety_Always_Enforced_In_All_Modes()
    {
        var schema = CreateSameDepthSchema();

        foreach (var mode in Enum.GetValues<OperationMergePolicy>())
        {
            var plan = PlanOperation(schema, SameDepthQuery, mode);
            Assert.NotNull(plan);
            Assert.NotEmpty(plan.AllNodes);
        }
    }

    [Fact]
    public void Default_MergePolicy_Is_Aggressive()
    {
        var options = new OperationPlannerOptions();
        Assert.Equal(OperationMergePolicy.Aggressive, options.MergePolicy);
    }

    [Fact]
    public void Snapshot_Aggressive_K6()
    {
        var schema = CreateK6Schema();
        var plan = PlanOperation(schema, K6Query, OperationMergePolicy.Aggressive);
        MatchSnapshot(plan);
    }

    [Fact]
    public void Snapshot_Conservative_K6()
    {
        var schema = CreateK6Schema();
        var plan = PlanOperation(schema, K6Query, OperationMergePolicy.Conservative);
        MatchSnapshot(plan);
    }

    [Fact]
    public void Snapshot_Balanced_K6()
    {
        var schema = CreateK6Schema();
        var plan = PlanOperation(schema, K6Query, OperationMergePolicy.Balanced);
        MatchSnapshot(plan);
    }

    private static List<BatchOperationDefinition> GetBatchOperationsForSchema(
        OperationPlan plan,
        string schemaName)
    {
        return plan.AllNodes
            .OfType<OperationBatchExecutionNode>()
            .Where(t => t.SchemaName == schemaName)
            .SelectMany(t => t.Operations.ToArray().OfType<BatchOperationDefinition>())
            .ToList();
    }

    private static OperationPlan PlanOperation(
        FusionSchemaDefinition schema,
        string operationText,
        OperationMergePolicy mergePolicy)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());

        var operationDoc = Utf8GraphQLParser.Parse(operationText);

        var rewriter = new Rewriters.DocumentRewriter(schema);
        var rewritten = rewriter.RewriteDocument(operationDoc, operationName: null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();

        var compiler = new OperationCompiler(schema, pool);
        var planner = new OperationPlanner(
            schema,
            compiler,
            new OperationPlannerOptions
            {
                MergePolicy = mergePolicy
            });
        const string id = "123456789101112";
        return planner.CreatePlan(id, id, id, operation);
    }

    // ── K6 schemas & query (used for snapshot tests) ────────────────────

    private static FusionSchemaDefinition CreateK6Schema()
        => ComposeSchema(
            FileResource.Open("k6-accounts.graphqls"),
            FileResource.Open("k6-products.graphqls"),
            FileResource.Open("k6-reviews.graphqls"),
            FileResource.Open("k6-inventory.graphqls"));

    private static readonly string K6Query = FileResource.Open("k6.graphql");

    // ── Deep cross-depth schema (depth diff=2, all 3 modes differ) ──────

    /// <summary>
    /// 4-schema chain producing identical productById lookups to "d"
    /// at depth 1 and depth 3 (diff=2):
    ///   a(root) → d(depth 1)  and  a → b(depth 1) → c(depth 2) → d(depth 3).
    /// </summary>
    private static FusionSchemaDefinition CreateDeepCrossDepthSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              product: Product
              warehouse: Warehouse
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Warehouse @key(fields: "id") {
              id: ID!
              name: String!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              warehouseById(id: ID! @is(field: "id")): Warehouse @lookup @internal
            }

            type Warehouse @key(fields: "id") {
              id: ID!
              shelf: Shelf
            }

            type Shelf @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: c
            schema {
              query: Query
            }

            type Query {
              shelfById(id: ID! @is(field: "id")): Shelf @lookup @internal
            }

            type Shelf @key(fields: "id") {
              id: ID!
              item: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: d
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              rating: Int!
            }
            """);
    }

    private const string DeepCrossDepthQuery =
        """
        {
          product {
            id
            rating
          }
          warehouse {
            id
            shelf {
              id
              item {
                id
                rating
              }
            }
          }
        }
        """;

    // ── Adjacent cross-depth schema (depth diff=1, Balanced merges) ─────

    private static FusionSchemaDefinition CreateCrossDepthSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              product: Product
              category: Category
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Category @key(fields: "id") {
              id: ID!
              name: String!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              categoryById(id: ID! @is(field: "id")): Category @lookup @internal
            }

            type Category @key(fields: "id") {
              id: ID!
              topProduct: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: c
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              rating: Int!
            }
            """);
    }

    private const string CrossDepthQuery =
        """
        {
          product {
            id
            rating
          }
          category {
            id
            topProduct {
              id
              rating
            }
          }
        }
        """;

    // ── Same-depth schema (all modes merge) ─────────────────────────────

    private static FusionSchemaDefinition CreateSameDepthSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              first: Product
              second: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              rating: Int!
            }
            """);
    }

    private const string SameDepthQuery =
        """
        {
          first {
            id
            rating
          }
          second {
            id
            rating
          }
        }
        """;
}

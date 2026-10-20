using System.Text;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public class OperationMergePolicyTests : FusionTestBase
{
    [Fact]
    public void Aggressive_Merges_Cross_Depth_Operations()
    {
        // arrange
        var schema = CreateDeepCrossDepthSchema();

        // act
        var plan = PlanOperation(schema, DeepCrossDepthQuery, OperationMergePolicy.Aggressive);

        // assert
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

        // assert
        var batchOps = GetBatchOperationsForSchema(plan, "d");
        Assert.DoesNotContain(batchOps, op => op.Targets.Length > 1);
    }

    [Fact]
    public void Balanced_Does_Not_Merge_Distant_Depth_Operations()
    {
        // arrange
        var schema = CreateDeepCrossDepthSchema();

        // act
        var plan = PlanOperation(schema, DeepCrossDepthQuery, OperationMergePolicy.Balanced);

        // assert
        var batchOps = GetBatchOperationsForSchema(plan, "d");
        Assert.DoesNotContain(batchOps, op => op.Targets.Length > 1);
    }

    [Fact]
    public void Balanced_Merges_Adjacent_Depth_Operations()
    {
        // arrange
        var schema = CreateCrossDepthSchema();

        // act
        var plan = PlanOperation(schema, CrossDepthQuery, OperationMergePolicy.Balanced);

        // assert
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

        // assert
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
    public void Merge_Should_Dedup_Into_Single_Batch_When_Forwarded_Literal_Is_MultiByte()
    {
        // arrange
        // The canonical plan signature is computed over UTF-8 bytes. A multi-byte
        // literal ("café") in the forwarded selection combined with the
        // __fusion_N_ variable-prefix rewriting would break dedup if the signature
        // used character indexing instead of byte indexing.
        var schema = CreateMultiByteLiteralSchema();

        // act
        var plan = PlanOperation(schema, MultiByteLiteralQuery, OperationMergePolicy.Aggressive);

        // assert
        var batchOps = GetBatchOperationsForSchema(plan, "b");
        Assert.Single(batchOps);
        Assert.Equal(2, batchOps[0].Targets.Length);
        Assert.Contains("café", Encoding.UTF8.GetString(batchOps[0].SourceText.Value.Span));
    }

    [Fact]
    public void Balanced_Produces_More_Nodes_Than_Aggressive_For_Distant_Depths()
    {
        // arrange
        var schema = CreateDeepCrossDepthSchema();

        // act
        var aggressivePlan = PlanOperation(schema, DeepCrossDepthQuery, OperationMergePolicy.Aggressive);
        var balancedPlan = PlanOperation(schema, DeepCrossDepthQuery, OperationMergePolicy.Balanced);

        // assert
        var aggressiveNodeCount = aggressivePlan.AllNodes.Length;
        var balancedNodeCount = balancedPlan.AllNodes.Length;
        Assert.True(
            balancedNodeCount > aggressiveNodeCount,
            $"Expected Balanced ({balancedNodeCount} nodes) > Aggressive ({aggressiveNodeCount} nodes)");
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
        var plan = PlanOperation(schema, s_k6Query, OperationMergePolicy.Aggressive);
        MatchSnapshot(plan);
    }

    [Fact]
    public void Snapshot_Conservative_K6()
    {
        var schema = CreateK6Schema();
        var plan = PlanOperation(schema, s_k6Query, OperationMergePolicy.Conservative);
        MatchSnapshot(plan);
    }

    [Fact]
    public void Snapshot_Balanced_K6()
    {
        var schema = CreateK6Schema();
        var plan = PlanOperation(schema, s_k6Query, OperationMergePolicy.Balanced);
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

        var rewriter = new DocumentRewriter(schema);
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

    private static FusionSchemaDefinition CreateK6Schema()
        => ComposeSchema(
            FileResource.Open("k6-accounts.graphqls"),
            FileResource.Open("k6-products.graphqls"),
            FileResource.Open("k6-reviews.graphqls"),
            FileResource.Open("k6-inventory.graphqls"));

    private static readonly string s_k6Query = FileResource.Open("k6.graphql");

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

    private static FusionSchemaDefinition CreateMultiByteLiteralSchema()
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
              translation(language: String!): String!
            }
            """);
    }

    private const string MultiByteLiteralQuery =
        """
        {
          first {
            id
            translation(language: "café")
          }
          second {
            id
            translation(language: "café")
          }
        }
        """;
    [Fact]
    public void Plan_Should_KeepIdenticalLookupsSeparate_When_MergePolicyIsNoneAndGroupingIsDisabled()
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { topProduct { name } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.None,
                EnableRequestGrouping = false
            });

        // assert
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        Assert.Equal(2, nodes.Length);
        Assert.All(nodes, node => Assert.IsType<OperationExecutionNode>(node));
    }

    [Fact]
    public void Plan_Should_BatchSeparateLookups_When_MergePolicyIsNone()
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { topProduct { name } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.None,
                EnableRequestGrouping = true
            });

        // assert
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        var operations = nodes.OfType<OperationBatchExecutionNode>()
            .SelectMany(n => n.Operations.ToArray()).ToArray();
        Assert.Single(nodes);
        Assert.Equal(2, operations.Length);
        Assert.All(operations, operation => Assert.IsType<SingleOperationDefinition>(operation));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_MergeSameDepthLookups_When_MergePolicyIsConservative(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { topProduct { name } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.Conservative,
                EnableRequestGrouping = enableRequestGrouping
            });

        // assert
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        var operations = nodes.OfType<OperationBatchExecutionNode>()
            .SelectMany(n => n.Operations.ToArray()).ToArray();
        Assert.Single(nodes);
        Assert.Single(operations);
        Assert.Contains(operations, operation => operation is BatchOperationDefinition { Targets.Length: 2 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_MergeSameDepthLookups_When_MergePolicyIsBalanced(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { topProduct { name } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.Balanced,
                EnableRequestGrouping = enableRequestGrouping
            });

        // assert
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        var operations = nodes.OfType<OperationBatchExecutionNode>()
            .SelectMany(n => n.Operations.ToArray()).ToArray();
        Assert.Single(nodes);
        Assert.Single(operations);
        Assert.Contains(operations, operation => operation is BatchOperationDefinition { Targets.Length: 2 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_MergeSameDepthLookups_When_MergePolicyIsAggressive(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { topProduct { name } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.Aggressive,
                EnableRequestGrouping = enableRequestGrouping
            });

        // assert
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        var operations = nodes.OfType<OperationBatchExecutionNode>()
            .SelectMany(n => n.Operations.ToArray()).ToArray();
        Assert.Single(nodes);
        Assert.Single(operations);
        Assert.Contains(operations, operation => operation is BatchOperationDefinition { Targets.Length: 2 });
    }

    [Fact]
    public void Plan_Should_KeepUnrelatedLookupsSeparate_When_RequestGroupingIsDisabled()
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { topProduct { name } }
              other: products { price }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.Aggressive,
                EnableRequestGrouping = false
            });

        // assert
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        var operations = nodes.OfType<OperationBatchExecutionNode>()
            .SelectMany(n => n.Operations.ToArray()).ToArray();
        Assert.Equal(2, nodes.Length);
        Assert.Single(operations);
        Assert.Contains(operations, operation => operation is BatchOperationDefinition { Targets.Length: 2 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_KeepAdjacentDepthLookupsSeparate_When_MergePolicyIsNone(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { featured { name } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.None,
                EnableRequestGrouping = enableRequestGrouping
            });

        // assert
        MatchSnapshot(plan);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_KeepDistantDepthLookupsSeparate_When_MergePolicyIsNone(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { collection { featured { name } } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.None,
                EnableRequestGrouping = enableRequestGrouping
            });

        // assert
        MatchSnapshot(plan);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_KeepAdjacentDepthLookupsSeparate_When_MergePolicyIsConservative(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { featured { name } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.Conservative,
                EnableRequestGrouping = enableRequestGrouping
            });

        // assert
        MatchSnapshot(plan);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_KeepDistantDepthLookupsSeparate_When_MergePolicyIsConservative(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { collection { featured { name } } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.Conservative,
                EnableRequestGrouping = enableRequestGrouping
            });

        // assert
        MatchSnapshot(plan);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_MergeAdjacentDepthLookups_When_MergePolicyIsBalanced(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { featured { name } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.Balanced,
                EnableRequestGrouping = enableRequestGrouping
            });

        // assert
        MatchSnapshot(plan);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_KeepDistantDepthLookupsSeparate_When_MergePolicyIsBalanced(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { collection { featured { name } } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.Balanced,
                EnableRequestGrouping = enableRequestGrouping
            });

        // assert
        MatchSnapshot(plan);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_MergeAdjacentDepthLookups_When_MergePolicyIsAggressive(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { featured { name } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.Aggressive,
                EnableRequestGrouping = enableRequestGrouping
            });

        // assert
        MatchSnapshot(plan);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_MergeDistantDepthLookups_When_MergePolicyIsAggressive(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands { collection { featured { name } } }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.Aggressive,
                EnableRequestGrouping = enableRequestGrouping
            });

        // assert
        MatchSnapshot(plan);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_KeepDistantLookupsSeparate_When_BalancedMergesPassThroughAdjacentDepths(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = FusionTestBase.PlanOperation(
            schema,
            """
            {
              products { name }
              brands {
                featured { name }
                collection { featured { name } }
              }
            }
            """,
            new OperationPlannerOptions
            {
                MergePolicy = OperationMergePolicy.Balanced,
                EnableRequestGrouping = enableRequestGrouping
            });

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateLookupPolicySchema()
        => ComposeSchema(
            """
            type Query {
              products: [Product!]!
              brands: [Brand!]!
            }

            type Product @key(fields: "id") { id: ID! }

            type Brand @key(fields: "id") {
              id: ID!
              topProduct: Product!
            }
            """,
            """
            type Query {
              brandById(id: ID! @is(field: "id")): Brand @lookup @internal
            }

            type Brand @key(fields: "id") {
              id: ID!
              featured: Product!
              collection: Collection!
            }

            type Collection @key(fields: "id") { id: ID! }

            type Product @key(fields: "id") { id: ID! }
            """,
            """
            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String!
              price: Int!
            }
            """,
            """
            type Query {
              collectionById(id: ID! @is(field: "id")): Collection @lookup @internal
            }

            type Collection @key(fields: "id") {
              id: ID!
              featured: Product!
            }

            type Product @key(fields: "id") { id: ID! }
            """);
}

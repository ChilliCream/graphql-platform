using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class ApolloLookupMergePlanningTests : FusionTestBase
{
    private const string Catalog =
        """
        extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

        type Query {
          products: [Product!]!
          brands: [Brand!]!
        }

        type Product @key(fields: "id") {
          id: ID!
        }

        type Brand @key(fields: "id") {
          id: ID!
          topProduct: Product!
        }
        """;

    private const string Featured =
        """
        extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

        type Brand @key(fields: "id") {
          id: ID!
          featured: Product!
        }

        type Product @key(fields: "id") {
          id: ID!
        }
        """;

    private const string Details =
        """
        extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

        type Product @key(fields: "id") {
          id: ID!
          name: String!
        }
        """;

    [Fact]
    public void Plan_Should_MergeApolloLookups_When_IdenticalLookupsShareDepth()
    {
        // arrange
        var schema = ComposeSchema(Catalog, Featured, Details);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                name
              }
              brands {
                topProduct {
                  name
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_KeepApolloLookupsSeparate_When_IdenticalLookupsDifferInDepth()
    {
        // arrange
        var schema = ComposeSchema(Catalog, Featured, Details);

        // act
        // 'brands.featured' is only known after the Featured lookup, so its Details
        // lookup sits one level deeper than the one for 'products'
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                name
              }
              brands {
                featured {
                  name
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }
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
        Assert.All(nodes, node => Assert.IsType<ApolloOperationExecutionNode>(node));
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
        var operations = nodes.OfType<ApolloOperationBatchExecutionNode>()
            .SelectMany(n => n.Operations.ToArray()).ToArray();
        Assert.Single(nodes);
        Assert.Equal(2, operations.Length);
        Assert.All(operations, operation => Assert.IsType<SingleOperationDefinition>(operation));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_MergeSameDepthLookups_When_MergePolicyIsConservative(bool grouping)
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
                EnableRequestGrouping = grouping
            });

        // assert
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        var operations = nodes.OfType<ApolloOperationBatchExecutionNode>()
            .SelectMany(n => n.Operations.ToArray()).ToArray();
        Assert.Single(nodes);
        Assert.Single(operations);
        Assert.Contains(operations, operation => operation is BatchOperationDefinition { Targets.Length: 2 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_MergeSameDepthLookups_When_MergePolicyIsBalanced(bool grouping)
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
                EnableRequestGrouping = grouping
            });

        // assert
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        var operations = nodes.OfType<ApolloOperationBatchExecutionNode>()
            .SelectMany(n => n.Operations.ToArray()).ToArray();
        Assert.Single(nodes);
        Assert.Single(operations);
        Assert.Contains(operations, operation => operation is BatchOperationDefinition { Targets.Length: 2 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_MergeSameDepthLookups_When_MergePolicyIsAggressive(bool grouping)
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
                EnableRequestGrouping = grouping
            });

        // assert
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        var operations = nodes.OfType<ApolloOperationBatchExecutionNode>()
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
        var operations = nodes.OfType<ApolloOperationBatchExecutionNode>()
            .SelectMany(n => n.Operations.ToArray()).ToArray();
        Assert.Equal(2, nodes.Length);
        Assert.Single(operations);
        Assert.Contains(operations, operation => operation is BatchOperationDefinition { Targets.Length: 2 });
    }

    private static FusionSchemaDefinition CreateLookupPolicySchema()
        => ComposeSchema(
            """
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

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
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Brand @key(fields: "id") {
              id: ID!
              featured: Product!
            }

            type Product @key(fields: "id") { id: ID! }
            """,
            """
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Product @key(fields: "id") {
              id: ID!
              name: String!
              price: Int!
            }
            """);
}

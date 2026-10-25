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

    private const string Related =
        """
        extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

        type Product @key(fields: "id") {
          id: ID!
          related: Product!
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

    private const string CategorizedDetails =
        """
        extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

        type Product @key(fields: "id") {
          id: ID!
          name: String!
          category: Category!
        }

        type Category @key(fields: "id") {
          id: ID!
        }
        """;

    private const string Categories =
        """
        extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

        type Category @key(fields: "id") {
          id: ID!
          label: String!
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
    public void Plan_Should_MergeApolloLookupsAcrossDepths_When_ShallowerLookupHasNoDependents()
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
    public void Plan_Should_KeepApolloLookupsSeparate_When_ShallowerLookupHasDependents()
    {
        // arrange
        var schema = ComposeSchema(Catalog, Featured, CategorizedDetails, Categories);

        // act
        // the Categories lookup for 'products.category' depends on the shallower
        // Details lookup, and the deeper Categories lookup does not wait for it
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                name
                category {
                  label
                }
              }
              brands {
                featured {
                  name
                  category {
                    label
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_KeepApolloLookupsSeparate_When_DeeperLookupIsDeferred()
    {
        // arrange
        var schema = ComposeSchema(Catalog, Related, Details);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                name
                related {
                  ... @defer(label: "deeper") { name }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_KeepApolloLookupsSeparate_When_LookupsBelongToSiblingDefers()
    {
        // arrange
        var schema = ComposeSchema(Catalog, Related, Details);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                ... @defer(label: "shallower") { name }
                related {
                  ... @defer(label: "deeper") { name }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_KeepApolloLookupsSeparate_When_DeeperLookupBelongsToNestedDefer()
    {
        // arrange
        var schema = ComposeSchema(Catalog, Related, Details);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                ... @defer(label: "outer") {
                  name
                  related {
                    id
                    ... @defer(label: "inner") { name }
                  }
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
        var plan = PlanOperation(
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
        var plan = PlanOperation(
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
    public void Plan_Should_MergeSameDepthLookups_When_MergePolicyIsConservative(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = PlanOperation(
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
        var operations = nodes.OfType<ApolloOperationBatchExecutionNode>()
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
        var plan = PlanOperation(
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
        var operations = nodes.OfType<ApolloOperationBatchExecutionNode>()
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
        var plan = PlanOperation(
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
        var plan = PlanOperation(
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_KeepAdjacentDepthLookupsSeparate_When_MergePolicyIsNone(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = PlanOperation(
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
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        Assert.Equal(2, nodes.Length);
        Assert.All(nodes, node => Assert.IsType<ApolloOperationExecutionNode>(node));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_KeepDistantDepthLookupsSeparate_When_MergePolicyIsNone(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = PlanOperation(
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
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        Assert.Equal(2, nodes.Length);
        Assert.All(nodes, node => Assert.IsType<ApolloOperationExecutionNode>(node));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_KeepAdjacentDepthLookupsSeparate_When_MergePolicyIsConservative(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = PlanOperation(
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
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        Assert.Equal(2, nodes.Length);
        Assert.All(nodes, node => Assert.IsType<ApolloOperationExecutionNode>(node));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_KeepDistantDepthLookupsSeparate_When_MergePolicyIsConservative(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = PlanOperation(
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
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        Assert.Equal(2, nodes.Length);
        Assert.All(nodes, node => Assert.IsType<ApolloOperationExecutionNode>(node));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_MergeAdjacentDepthLookups_When_MergePolicyIsBalanced(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = PlanOperation(
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
    public void Plan_Should_KeepDistantDepthLookupsSeparate_When_MergePolicyIsBalanced(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = PlanOperation(
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
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        Assert.Equal(2, nodes.Length);
        Assert.All(nodes, node => Assert.IsType<ApolloOperationExecutionNode>(node));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_Should_MergeAdjacentDepthLookups_When_MergePolicyIsAggressive(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = PlanOperation(
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
    public void Plan_Should_MergeDistantDepthLookups_When_MergePolicyIsAggressive(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = PlanOperation(
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
    public void Plan_Should_KeepDistantLookupsSeparate_When_BalancedMergesPassThroughAdjacentDepths(bool enableRequestGrouping)
    {
        // arrange
        var schema = CreateLookupPolicySchema();

        // act
        var plan = PlanOperation(
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
        var nodes = plan.AllNodes.Where(n => n.SchemaName == "c").ToArray();
        var operations = nodes.OfType<ApolloOperationBatchExecutionNode>()
            .SelectMany(n => n.Operations.ToArray()).ToArray();
        Assert.Equal(2, nodes.Length);
        var merged = Assert.IsType<BatchOperationDefinition>(Assert.Single(operations));
        Assert.Equal(
            ["$.brands.featured", "$.products"],
            merged.Targets.ToArray().Select(t => t.ToString()).Order(StringComparer.Ordinal));
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
              collection: Collection!
            }

            type Collection @key(fields: "id") { id: ID! }

            type Product @key(fields: "id") { id: ID! }
            """,
            """
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Product @key(fields: "id") {
              id: ID!
              name: String!
              price: Int!
            }
            """,
            """
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Collection @key(fields: "id") {
              id: ID!
              featured: Product!
            }

            type Product @key(fields: "id") { id: ID! }
            """);
}

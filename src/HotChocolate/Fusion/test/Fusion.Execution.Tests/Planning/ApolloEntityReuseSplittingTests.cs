using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public sealed class ApolloEntityReuseSplittingTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_KeepIdenticalApolloLookupsApart_When_EntityReuseCanServeDeeperLookup()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var plan = PlanOperation(schema, Query);

        // assert
        DescribeLookups(plan, "inventory").MatchInlineSnapshot(
            """
            3: $.topProducts depends on [1]
            5: $.topProducts.reviews.product depends on [2, 4] runs after [3]
            """);
    }

    [Fact]
    public void Parse_Should_RestoreApolloOrderingDependency_When_SplitPlanIsFormatted()
    {
        // arrange
        var schema = CreateSchema();
        var plan = PlanOperation(schema, Query);
        using var buffer = new PooledArrayWriter();
        new JsonOperationPlanFormatter().Format(buffer, plan);
        var parser = new JsonOperationPlanParser(
            new OperationCompiler(
                schema,
                new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                    new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>())));

        // act
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        DescribeLookups(parsedPlan, "inventory").MatchInlineSnapshot(
            """
            3: $.topProducts depends on [1]
            5: $.topProducts.reviews.product depends on [2, 4] runs after [3]
            """);
    }

    [Fact]
    public void Plan_Should_MergeIdenticalApolloLookups_When_EntityReuseSplittingIsDisabled()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var plan = PlanOperation(
            schema,
            Query,
            new OperationPlannerOptions { EnableEntityReuseSplitting = false });

        // assert
        DescribeLookups(plan, "inventory").MatchInlineSnapshot(
            """
            3: $.topProducts, $.topProducts.reviews.product depends on [1, 2, 4]
            """);
    }

    private const string Query =
        """
        {
          topProducts {
            name
            inStock
            shippingEstimate
            reviews {
              product {
                name
                inStock
                shippingEstimate
              }
            }
          }
        }
        """;

    private static string DescribeLookups(OperationPlan plan, string schemaName)
    {
        var description = new StringBuilder();

        foreach (var node in plan.AllNodes)
        {
            switch (node)
            {
                case ApolloOperationExecutionNode operation when operation.SchemaName == schemaName:
                    Describe(
                        description,
                        operation.Id,
                        [operation.Target],
                        operation.Dependencies,
                        operation.OptionalDependencies);
                    break;

                case ApolloOperationBatchExecutionNode batch when batch.SchemaName == schemaName:
                    foreach (var definition in batch.Operations)
                    {
                        var targets = definition switch
                        {
                            SingleOperationDefinition single => [single.Target],
                            BatchOperationDefinition merged => merged.Targets.ToArray(),
                            _ => []
                        };

                        Describe(
                            description,
                            definition.Id,
                            targets,
                            definition.Dependencies,
                            definition.OptionalDependencies);
                    }
                    break;
            }
        }

        return description.ToString().TrimEnd();
    }

    private static void Describe(
        StringBuilder description,
        int id,
        SelectionPath[] targets,
        ReadOnlySpan<IOperationPlanNode> dependencies,
        ReadOnlySpan<IOperationPlanNode> optionalDependencies)
    {
        description.Append(
            $"{id}: {string.Join(", ", targets.Select(t => t.ToString()))}"
            + $" depends on [{FormatIds(dependencies)}]");

        if (optionalDependencies.Length > 0)
        {
            description.Append($" runs after [{FormatIds(optionalDependencies)}]");
        }

        description.AppendLine();
    }

    private static string FormatIds(ReadOnlySpan<IOperationPlanNode> nodes)
    {
        var ids = new List<int>();

        foreach (var node in nodes)
        {
            ids.Add(node.Id);
        }

        return string.Join(", ", ids.Order());
    }

    private static FusionSchemaDefinition CreateSchema()
        => ComposeSchema(
            """
            # name: products
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.7", import: ["@key"])

            type Query {
              topProducts: [Product!]!
            }

            type Product @key(fields: "upc") {
              upc: String!
              name: String!
              price: Int!
              weight: Int!
            }
            """,
            """
            # name: inventory
            extend schema @link(
              url: "https://specs.apollo.dev/federation/v2.7"
              import: ["@external", "@key", "@requires"]
            )

            type Product @key(fields: "upc") {
              upc: String! @external
              price: Int! @external
              weight: Int! @external
              inStock: Boolean!
              shippingEstimate: Int @requires(fields: "price weight")
            }
            """,
            """
            # name: reviews
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.7", import: ["@external", "@key"])

            type Product @key(fields: "upc") {
              upc: String! @external
              reviews: [Review!]!
            }

            type Review @key(fields: "id") {
              id: ID!
              product: Product
            }
            """);
}

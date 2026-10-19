using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class EntityReuseSplittingTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_KeepIdenticalLookupsApart_When_EntityReuseCanServeDeeperLookup()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var plan = PlanOperation(schema, Query);

        // assert
        DescribeLookups(plan, "d").MatchInlineSnapshot(
            """
            4: $.warehouse.shelf.item depends on [3, 5]
            5: $.product depends on [1]
            """);
    }

    [Fact]
    public void Plan_Should_MergeIdenticalLookups_When_EntityReuseSplittingIsDisabled()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var plan = PlanOperation(
            schema,
            Query,
            new OperationPlannerOptions { EnableEntityReuseSplitting = false });

        // assert
        DescribeLookups(plan, "d").MatchInlineSnapshot(
            """
            4: $.warehouse.shelf.item, $.product depends on [1, 3]
            """);
    }

    [Fact]
    public void Plan_Should_MergeIdenticalLookups_When_LookupSelectsCompositeField()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              product {
                dimensions { width }
              }
              warehouse {
                shelf {
                  item {
                    dimensions { width }
                  }
                }
              }
            }
            """);

        // assert
        DescribeLookups(plan, "d").MatchInlineSnapshot(
            """
            4: $.warehouse.shelf.item, $.product depends on [1, 3]
            """);
    }

    private const string Query =
        """
        {
          product {
            rating
          }
          warehouse {
            shelf {
              item {
                rating
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
                case OperationExecutionNode operation when operation.SchemaName == schemaName:
                    Describe(description, operation.Id, [operation.Target], operation.Dependencies);
                    break;

                case OperationBatchExecutionNode batch when batch.SchemaName == schemaName:
                    foreach (var definition in batch.Operations)
                    {
                        var targets = definition switch
                        {
                            SingleOperationDefinition single => [single.Target],
                            BatchOperationDefinition merged => merged.Targets.ToArray(),
                            _ => []
                        };

                        Describe(description, definition.Id, targets, definition.Dependencies);
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
        ReadOnlySpan<IOperationPlanNode> dependencies)
    {
        var dependencyIds = new List<int>();

        foreach (var dependency in dependencies)
        {
            dependencyIds.Add(dependency.Id);
        }

        description.AppendLine(
            $"{id}: {string.Join(", ", targets.Select(t => t.ToString()))}"
            + $" depends on [{string.Join(", ", dependencyIds.Order())}]");
    }

    private static FusionSchemaDefinition CreateSchema()
        => ComposeSchema(
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
              dimensions: Dimensions!
            }

            type Dimensions {
              width: Int!
            }
            """);
}

using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class MutationSerialPlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_KeepEachEntityLookupWithItsRoot_When_ApolloMutationRootsSelectSameEntityField()
    {
        // arrange
        var schema = CreateApolloLedgerSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation Serial {
              a: bump(counter: "serial", by: 1) {
                value
              }
              b: bump(counter: "serial", by: 10) {
                value
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_KeepEachEntityLookupWithItsRoot_When_ApolloLookupsAreGroupedWithoutMerging()
    {
        // arrange
        var schema = CreateApolloLedgerSchema();
        var options = new OperationPlannerOptions { MergePolicy = OperationMergePolicy.None };

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation Serial {
              a: bump(counter: "serial", by: 1) {
                value
              }
              b: bump(counter: "serial", by: 10) {
                value
              }
            }
            """,
            options);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_KeepEachLookupWithItsRoot_When_CompositeMutationRootsSelectSameEntityField()
    {
        // arrange
        var schema = CreateCompositeLedgerSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation Serial {
              a: bump(counter: "serial", by: 1) {
                value
              }
              b: bump(counter: "serial", by: 10) {
                value
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_KeepEachLookupWithItsRoot_When_CompositeLookupsAreGroupedWithoutMerging()
    {
        // arrange
        var schema = CreateCompositeLedgerSchema();
        var options = new OperationPlannerOptions { MergePolicy = OperationMergePolicy.None };

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation Serial {
              a: bump(counter: "serial", by: 1) {
                value
              }
              b: bump(counter: "serial", by: 10) {
                value
              }
            }
            """,
            options);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateApolloLedgerSchema()
    {
        return ComposeSchema(
            """
            # name: ledger
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
              ping: String
            }

            type Mutation {
              bump(counter: ID!, by: Int!): Counter
            }

            type Counter @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: reader
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Counter @key(fields: "id") {
              id: ID!
              value: Int
            }
            """);
    }

    private static FusionSchemaDefinition CreateCompositeLedgerSchema()
    {
        return ComposeSchema(
            """
            # name: ledger
            type Query {
              ping: String
            }

            type Mutation {
              bump(counter: ID!, by: Int!): Counter
            }

            type Counter @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: reader
            type Query {
              counterById(id: ID!): Counter @lookup
            }

            type Counter @key(fields: "id") {
              id: ID!
              value: Int
            }
            """);
    }
}

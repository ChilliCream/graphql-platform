using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityRequireReentryTests
{
    [Theory]
    [InlineData("a", "b")]
    [InlineData("A", "B")]
    [InlineData("orderService", "customerService")]
    public void Compose_Should_Succeed_When_NestedRequiredFieldReentersRequiringSchema(
        string requiringSchemaName,
        string otherSchemaName)
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                requiringSchemaName,
                """
                type Query {
                    orderById(id: ID!): Order @lookup @internal
                    customerById(id: ID!): Customer @lookup @internal
                }

                type Order {
                    id: ID! @shareable
                    price(discount: Int @require(field: "customer.discount")): Int
                }

                type Customer {
                    id: ID! @shareable
                    discount: Int
                }
                """),
            new SourceSchemaText(
                otherSchemaName,
                """
                type Query {
                    orders: [Order]
                }

                type Order {
                    id: ID! @shareable
                    customer: Customer
                }

                type Customer {
                    id: ID! @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_TwoNestedRequiredLevelsReenterRequiringSchema()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "a",
                """
                type Query {
                    orderById(id: ID!): Order @lookup @internal
                    customerById(id: ID!): Customer @lookup @internal
                }

                type Order {
                    id: ID! @shareable
                    shippingCost(city: String @require(field: "customer.address.city")): Int
                }

                type Customer {
                    id: ID! @shareable
                    address: Address
                }

                type Address {
                    city: String
                }
                """),
            new SourceSchemaText(
                "b",
                """
                type Query {
                    orders: [Order]
                }

                type Order {
                    id: ID! @shareable
                    customer: Customer
                }

                type Customer {
                    id: ID! @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_NestedRequiredFieldReentersRequiringSchemaBelowInterface()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "a",
                """
                type Query {
                    orderById(id: ID!): Order @lookup @internal
                    customerById(id: ID!): Customer @lookup @internal
                }

                interface Person {
                    id: ID!
                    discount: Int
                }

                type Order {
                    id: ID! @shareable
                    price(discount: Int @require(field: "customer.discount")): Int
                }

                type Customer implements Person {
                    id: ID! @shareable
                    discount: Int
                }
                """),
            new SourceSchemaText(
                "b",
                """
                type Query {
                    orders: [Order]
                }

                interface Person {
                    id: ID!
                }

                type Order {
                    id: ID! @shareable
                    customer: Person
                }

                type Customer implements Person {
                    id: ID! @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Validate_Should_Fail_When_FirstLevelRequiredIntermediateIsOnlyInRequiringSchema()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    orders: [Order]
                    orderById(id: ID!): Order @lookup @internal
                }

                type Order {
                    id: ID! @shareable
                    customer: Customer
                    price(discount: Int @require(field: "customer.discount")): Int
                }

                type Customer {
                    id: ID! @shareable
                }
                """,
                """
                # Schema B
                type Query {
                    customerById(id: ID!): Customer @lookup @internal
                }

                type Customer {
                    id: ID! @shareable
                    discount: Int
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Order.price' on path 'A:Query.orders<Order>'.
              Unable to satisfy the requirement '{ customer { discount } }' on field 'A:Order.price<Int>'.
                Unable to satisfy the requirement 'customer { discount }'.
                  Unable to access the required field 'Order.customer' on path 'A:Query.orders<Order>'.
                    No other schemas contain the field 'Order.customer'.
            """
        ]);
    }

    [Theory]
    [InlineData(NodeResolution.Gateway)]
    [InlineData(NodeResolution.SourceSchema)]
    public void Compose_Should_Succeed_When_NodeBranchRequirementReentersRequiringSchema(
        NodeResolution nodeResolution)
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "a",
                """
                type Query {
                    node(id: ID!): Node @lookup @shareable
                }

                interface Node {
                    id: ID!
                }

                type Chat implements Node @key(fields: "id") {
                    id: ID!
                    title: String
                    summary(text: String @require(field: "text")): String
                }
                """),
            new SourceSchemaText(
                "b",
                """
                type Query {
                    chatById(id: ID!): Chat @lookup @internal
                }

                type Chat @key(fields: "id") {
                    id: ID! @external
                    text: String!
                }
                """)
        ];

        var log = new CompositionLog();
        var composer = new SchemaComposer(
            sourceSchemas,
            new SchemaComposerOptions
            {
                Merger =
                {
                    EnableGlobalObjectIdentification = true,
                    NodeResolution = nodeResolution
                },
                Satisfiability = { IncludeSatisfiabilityPaths = true }
            },
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }
}

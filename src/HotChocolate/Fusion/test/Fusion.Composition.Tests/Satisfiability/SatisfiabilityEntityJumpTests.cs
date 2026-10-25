using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityEntityJumpTests
{
    [Theory]
    [InlineData("A", "C", true)]
    [InlineData("A", "C", false)]
    [InlineData("C", "A", true)]
    [InlineData("C", "A", false)]
    public void Validate_Should_Succeed_When_TypeIsReachedByChainingLookupsWithDifferentKeys(
        string shipmentsSchemaName,
        string carriersSchemaName,
        bool idKeyFirst)
    {
        // arrange
        var (firstLookup, secondLookup) = idKeyFirst
            ? ("shipmentById(id: ID!): Shipment @lookup @internal",
                "shipmentByTrackingCode(trackingCode: String!): Shipment @lookup @internal")
            : ("shipmentByTrackingCode(trackingCode: String!): Shipment @lookup @internal",
                "shipmentById(id: ID!): Shipment @lookup @internal");
        var (firstKeyField, secondKeyField) = idKeyFirst
            ? ("id: ID! @shareable", "trackingCode: String! @shareable")
            : ("trackingCode: String! @shareable", "id: ID! @shareable");

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                shipmentsSchemaName,
                """
                type Query {
                    shipments: [Shipment]
                }

                type Shipment {
                    id: ID! @shareable
                    weight: Int
                }
                """),
            new SourceSchemaText(
                "B",
                $$"""
                type Query {
                    {{firstLookup}}
                    {{secondLookup}}
                }

                type Shipment {
                    {{firstKeyField}}
                    {{secondKeyField}}
                }
                """),
            new SourceSchemaText(
                carriersSchemaName,
                """
                type Query {
                    shipmentByTrackingCode(trackingCode: String!): Shipment @lookup @internal
                }

                type Shipment {
                    trackingCode: String! @shareable
                    carrier: String
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
    public void Validate_Should_Fail_When_NoSchemaOnPathProvidesSecondLookupKey()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    shipments: [Shipment]
                }

                type Shipment {
                    id: ID! @shareable
                    weight: Int
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    shipmentById(id: ID!): Shipment @lookup @internal
                }

                type Shipment {
                    id: ID! @shareable
                    label: String
                }
                """),
            new SourceSchemaText(
                "C",
                """
                type Query {
                    shipmentByTrackingCode(trackingCode: String!): Shipment @lookup @internal
                }

                type Shipment {
                    trackingCode: String!
                    carrier: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Shipment.trackingCode' on path 'A:Query.shipments<Shipment>'.
              Unable to transition between schemas 'A' and 'C' for access to field 'C:Shipment.trackingCode<String>'.
                Unable to satisfy the requirement '{ trackingCode }' for lookup 'shipmentByTrackingCode' in schema 'C'.
                  Unable to satisfy the requirement 'trackingCode'.
                    Unable to access the required field 'Shipment.trackingCode' on path 'A:Query.shipments<Shipment>'.
                      No other schemas contain the field 'Shipment.trackingCode'.

            Unable to access the field 'Shipment.carrier' on path 'A:Query.shipments<Shipment>'.
              Unable to transition between schemas 'A' and 'C' for access to field 'C:Shipment.carrier<String>'.
                Unable to satisfy the requirement '{ trackingCode }' for lookup 'shipmentByTrackingCode' in schema 'C'.
                  Unable to satisfy the requirement 'trackingCode'.
                    Unable to access the required field 'Shipment.trackingCode' on path 'A:Query.shipments<Shipment>'.
                      No other schemas contain the field 'Shipment.trackingCode'.
            """);
    }

    [Fact]
    public void Validate_Should_Fail_When_NestedEntityCannotReachOwningSchema()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    orders: [Order]
                }

                type Order {
                    id: ID!
                    customer: Customer
                }

                type Customer {
                    id: ID! @shareable
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    customerByEmail(email: String!): Customer @lookup
                }

                type Customer {
                    id: ID! @shareable
                    email: String!
                    name: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Customer.email' on path 'A:Query.orders<Order> -> A:Order.customer<Customer>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Customer.email<String>'.
                Unable to satisfy the requirement '{ email }' for lookup 'customerByEmail' in schema 'B'.
                  Unable to satisfy the requirement 'email'.
                    Unable to access the required field 'Customer.email' on path 'A:Order.customer<Customer>'.
                      No other schemas contain the field 'Customer.email'.

            Unable to access the field 'Customer.name' on path 'A:Query.orders<Order> -> A:Order.customer<Customer>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Customer.name<String>'.
                Unable to satisfy the requirement '{ email }' for lookup 'customerByEmail' in schema 'B'.
                  Unable to satisfy the requirement 'email'.
                    Unable to access the required field 'Customer.email' on path 'A:Order.customer<Customer>'.
                      No other schemas contain the field 'Customer.email'.
            """);
    }
}

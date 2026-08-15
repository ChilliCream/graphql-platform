using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.PreMergeValidationRules;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class ApolloFederationEnumValuesMismatchRuleTests : RuleTestBase
{
    protected override object Rule { get; } =
        new EnumValuesMismatchRule(EnumValuesMergeBehavior.Auto);

    // Here, two Apollo subgraphs define "OrderPriority" with differing values, but the enum is
    // only used in output positions, so Apollo composition merges the values by union.
    [Fact]
    public void Validate_Should_Succeed_When_OutputOnlyEnumValuesDifferAcrossApolloSubgraphs()
    {
        AssertValid(
        [
            """
            extend schema
                @link(
                    url: "https://specs.apollo.dev/federation/v2.3"
                    import: ["@key"])

            type Query {
                orderById: Order
            }

            type Order @key(fields: "id") {
                id: ID!
                priority: OrderPriority
            }

            enum OrderPriority {
                LOW
                HIGH
                RUSH
            }
            """,
            """
            extend schema
                @link(
                    url: "https://specs.apollo.dev/federation/v2.3"
                    import: ["@key"])

            type Query {
                trackedOrder: Order
            }

            type Order @key(fields: "id") {
                id: ID!
                fulfillmentPriority: OrderPriority
            }

            enum OrderPriority {
                LOW
                HIGH
            }
            """
        ]);
    }

    // Here, the enum is used as a field argument type in one Apollo subgraph, so the
    // exact-agreement requirement applies and the differing values are reported.
    [Fact]
    public void Validate_Should_Fail_When_InputUsedEnumValuesDifferAcrossApolloSubgraphs()
    {
        AssertInvalid(
            [
                """
                extend schema
                    @link(
                        url: "https://specs.apollo.dev/federation/v2.3"
                        import: ["@key"])

                type Query {
                    orderById: Order
                    ordersByPriority(priority: OrderPriority!): [Order!]
                }

                type Order @key(fields: "id") {
                    id: ID!
                    priority: OrderPriority
                }

                enum OrderPriority {
                    LOW
                    HIGH
                    RUSH
                }
                """,
                """
                extend schema
                    @link(
                        url: "https://specs.apollo.dev/federation/v2.3"
                        import: ["@key"])

                type Query {
                    trackedOrder: Order
                }

                type Order @key(fields: "id") {
                    id: ID!
                    fulfillmentPriority: OrderPriority
                }

                enum OrderPriority {
                    LOW
                    HIGH
                }
                """
            ],
            [
                """
                {
                    "message": "The enum type 'OrderPriority' in schema 'B' must define the value 'RUSH'.",
                    "code": "ENUM_VALUES_MISMATCH",
                    "severity": "Error",
                    "coordinate": "OrderPriority",
                    "member": "OrderPriority",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }

    // Here, the enum-typed field is part of the entity key, so the generated internal lookup
    // field uses the enum as an argument type and the differing values are reported.
    [Fact]
    public void Validate_Should_Fail_When_KeyFieldEnumValuesDifferAcrossApolloSubgraphs()
    {
        AssertInvalid(
            [
                """
                extend schema
                    @link(
                        url: "https://specs.apollo.dev/federation/v2.3"
                        import: ["@key"])

                type Query {
                    orderById: Order
                }

                type Order @key(fields: "id priority") {
                    id: ID!
                    priority: OrderPriority
                }

                enum OrderPriority {
                    LOW
                    HIGH
                    RUSH
                }
                """,
                """
                extend schema
                    @link(
                        url: "https://specs.apollo.dev/federation/v2.3"
                        import: ["@key"])

                type Query {
                    trackedOrder: Order
                }

                type Order @key(fields: "id priority") {
                    id: ID!
                    priority: OrderPriority
                }

                enum OrderPriority {
                    LOW
                    HIGH
                }
                """
            ],
            [
                """
                {
                    "message": "The enum type 'OrderPriority' in schema 'B' must define the value 'RUSH'.",
                    "code": "ENUM_VALUES_MISMATCH",
                    "severity": "Error",
                    "coordinate": "OrderPriority",
                    "member": "OrderPriority",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }

    // Here, one Apollo subgraph and one plain subgraph define the enum with differing values.
    // The Apollo subgraph resolves the automatic merge behavior to union for the whole
    // composition, so the output-only enum composes.
    [Fact]
    public void Validate_Should_Succeed_When_OutputOnlyEnumValuesDifferAcrossMixedSubgraphs()
    {
        AssertValid(
        [
            """
            extend schema
                @link(
                    url: "https://specs.apollo.dev/federation/v2.3"
                    import: ["@key"])

            type Query {
                orderById: Order
            }

            type Order @key(fields: "id") {
                id: ID!
                priority: OrderPriority
            }

            enum OrderPriority {
                LOW
                HIGH
                RUSH
            }
            """,
            """
            type Query {
                archivedOrder: Order
            }

            type Order {
                id: ID!
                priority: OrderPriority
            }

            enum OrderPriority {
                LOW
                HIGH
            }
            """
        ]);
    }

    // Here, two plain subgraphs define the enum with differing values while a third Apollo
    // subgraph does not define it at all. The Apollo subgraph still resolves the automatic
    // merge behavior to union for the whole composition.
    [Fact]
    public void Validate_Should_Succeed_When_OutputOnlyEnumValuesDifferAndAnyApolloSubgraphPresent()
    {
        AssertValid(
        [
            """
            type Query {
                orderById: Order
            }

            type Order {
                id: ID!
                priority: OrderPriority
            }

            enum OrderPriority {
                LOW
                HIGH
                RUSH
            }
            """,
            """
            type Query {
                archivedOrder: Order
            }

            type Order {
                id: ID!
                priority: OrderPriority
            }

            enum OrderPriority {
                LOW
                HIGH
            }
            """,
            """
            extend schema
                @link(
                    url: "https://specs.apollo.dev/federation/v2.3"
                    import: ["@key"])

            type Query {
                shipmentById: Shipment
            }

            type Shipment @key(fields: "id") {
                id: ID!
                carrier: String
            }
            """
        ]);
    }

    // Here, the explicit strict merge behavior overrules the Apollo subgraph detection, so the
    // differing values are reported.
    [Fact]
    public void Validate_Should_Fail_When_OutputOnlyEnumValuesDifferAndStrictMergeBehavior()
    {
        AssertInvalid(
            [
                """
                extend schema
                    @link(
                        url: "https://specs.apollo.dev/federation/v2.3"
                        import: ["@key"])

                type Query {
                    orderById: Order
                }

                type Order @key(fields: "id") {
                    id: ID!
                    priority: OrderPriority
                }

                enum OrderPriority {
                    LOW
                    HIGH
                    RUSH
                }
                """,
                """
                extend schema
                    @link(
                        url: "https://specs.apollo.dev/federation/v2.3"
                        import: ["@key"])

                type Query {
                    trackedOrder: Order
                }

                type Order @key(fields: "id") {
                    id: ID!
                    fulfillmentPriority: OrderPriority
                }

                enum OrderPriority {
                    LOW
                    HIGH
                }
                """
            ],
            [
                """
                {
                    "message": "The enum type 'OrderPriority' in schema 'B' must define the value 'RUSH'.",
                    "code": "ENUM_VALUES_MISMATCH",
                    "severity": "Error",
                    "coordinate": "OrderPriority",
                    "member": "OrderPriority",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ],
            new EnumValuesMismatchRule(EnumValuesMergeBehavior.Strict));
    }

    // Here, the explicit union merge behavior applies even though no Apollo subgraph is part of
    // the composition, so the output-only enum composes.
    [Fact]
    public void Validate_Should_Succeed_When_OutputOnlyEnumValuesDifferAndUnionMergeBehavior()
    {
        AssertValid(
            [
                """
                type Query {
                    orderById: Order
                }

                type Order {
                    id: ID!
                    priority: OrderPriority
                }

                enum OrderPriority {
                    LOW
                    HIGH
                    RUSH
                }
                """,
                """
                type Query {
                    archivedOrder: Order
                }

                type Order {
                    id: ID!
                    priority: OrderPriority
                }

                enum OrderPriority {
                    LOW
                    HIGH
                }
                """
            ],
            new EnumValuesMismatchRule(EnumValuesMergeBehavior.Union));
    }

    // Here, the enum is used as a field argument type, so the exact-agreement requirement
    // applies even under the explicit union merge behavior.
    [Fact]
    public void Validate_Should_Fail_When_InputUsedEnumValuesDifferAndUnionMergeBehavior()
    {
        AssertInvalid(
            [
                """
                type Query {
                    orderById: Order
                    ordersByPriority(priority: OrderPriority!): [Order!]
                }

                type Order {
                    id: ID!
                    priority: OrderPriority
                }

                enum OrderPriority {
                    LOW
                    HIGH
                    RUSH
                }
                """,
                """
                type Query {
                    archivedOrder: Order
                }

                type Order {
                    id: ID!
                    priority: OrderPriority
                }

                enum OrderPriority {
                    LOW
                    HIGH
                }
                """
            ],
            [
                """
                {
                    "message": "The enum type 'OrderPriority' in schema 'B' must define the value 'RUSH'.",
                    "code": "ENUM_VALUES_MISMATCH",
                    "severity": "Error",
                    "coordinate": "OrderPriority",
                    "member": "OrderPriority",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ],
            new EnumValuesMismatchRule(EnumValuesMergeBehavior.Union));
    }
}

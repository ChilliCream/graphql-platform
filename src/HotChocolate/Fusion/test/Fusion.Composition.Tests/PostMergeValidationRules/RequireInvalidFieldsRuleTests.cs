using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class RequireInvalidFieldsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new RequireInvalidFieldsRule();

    // In the following example, the @require directive's "field" argument is a valid selection set
    // and satisfies the rule.
    [Fact]
    public void Validate_RequireValidFields_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type User @key(fields: "id") {
                id: ID!
                profile(name: String @require(field: "name")): Profile
            }

            type Profile {
                id: ID!
                name: String
            }
            """,
            """
            # Schema B
            type User @key(fields: "id") {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // In this example, the "field" argument references fields from another source schema.
    [Fact]
    public void Validate_RequireValidFieldsAcrossSchemas_Succeeds()
    {
        AssertValid(
        [
            """
            type Product {
                id: ID!
                delivery(
                    zip: String!
                    dimension: ProductDimensionInput!
                        @require(field: "{ productSize: dimension.size, productWeight: dimension.weight }")
                ): DeliveryEstimates
            }

            input ProductDimensionInput {
                productSize: Int!
                productWeight: Int!
            }
            """,
            """
            type Product {
                dimension: ProductDimension!
            }

            type ProductDimension {
                size: Int!
                weight: Int!
            }
            """
        ]);
    }

    // In the following example, the @require directive's "field" argument references a field with a
    // list type.
    [Fact]
    public void Validate_RequireValidFieldsListType_Succeeds()
    {
        AssertValid(
        [
            """
            type CartDiscount {
                channels(channelIds: [ID!]! @require(field: "channelIds")): ChannelConnection!
                id: ID!
            }
            """,
            """
            type CartDiscount {
                id: ID!
                name: String!
                channelIds: [ID!]!
            }
            """
        ]);
    }

    // In the following example, the @require directive traverses a list-typed intermediate
    // ("lines") using FieldSelectionMap list syntax ("lines[{ ... }]"). This is the shape generated
    // for an Apollo @requires whose selection path crosses a list, and it satisfies the rule.
    [Fact]
    public void Validate_RequireValidFieldsNestedListIntermediate_Succeeds()
    {
        AssertValid(
        [
            """
            type Order @key(fields: "id") {
                id: ID!
                summary(
                    info: OrderInfoInput
                        @require(field: "{ lines: info.lines[{ sku: sku }] }")
                ): Boolean
            }

            input OrderInfoInput {
                lines: [OrderLineInput]
            }

            input OrderLineInput {
                sku: ID
            }
            """,
            """
            type Order @key(fields: "id") {
                id: ID!
                info: Info
            }

            type Info {
                lines: [Line]
            }

            type Line {
                sku: ID
            }
            """
        ]);
    }

    // In this example, the @require directive references a field ("unknownField") that does not
    // exist on the parent type ("Book"), causing a REQUIRE_INVALID_FIELDS error.
    [Fact]
    public void Validate_RequireInvalidFields_Fails()
    {
        AssertInvalid(
            [
                """
                type Book {
                    id: ID!
                    pages(pageSize: Int @require(field: "unknownField")): Int
                }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Book.pages(pageSize:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "REQUIRE_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Book.pages(pageSize:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The field 'unknownField' does not exist on the type 'Book'."
                        ]
                    }
                }
                """
            ]);
    }

    // In the following example, the @require directive references a field from itself (Book.size)
    // which is not allowed. This results in a REQUIRE_INVALID_FIELDS error.
    [Fact]
    public void Validate_RequireInvalidFieldsReferenceToLocalField_Fails()
    {
        AssertInvalid(
            [
                """
                type Book {
                    id: ID!
                    size: Int
                    pages(pageSize: Int @require(field: "size")): Int
                }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Book.pages(pageSize:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "REQUIRE_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Book.pages(pageSize:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The required field 'Book.size' must not be served by the requiring source schema 'A'."
                        ]
                    }
                }
                """
            ]);
    }

    // A constant argument on a required field is valid when it matches the field's argument
    // definition.
    [Fact]
    public void Validate_RequireFieldWithValidArgument_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Product @key(fields: "id") {
                id: ID!
                shippingCost(size: Int! @require(field: "dimension(unit: METRIC)")): Float
            }
            """,
            """
            # Schema B
            type Product @key(fields: "id") {
                id: ID!
                dimension(unit: Unit!): Int!
            }

            enum Unit { METRIC IMPERIAL }
            """
        ]);
    }

    // The @require field selection map references a constant argument that does not exist on the
    // field.
    [Fact]
    public void Validate_RequireFieldWithUnknownArgument_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Product @key(fields: "id") {
                    id: ID!
                    shippingCost(size: Int! @require(field: "dimension(scale: METRIC)")): Float
                }
                """,
                """
                # Schema B
                type Product @key(fields: "id") {
                    id: ID!
                    dimension(unit: Unit!): Int!
                }

                enum Unit { METRIC IMPERIAL }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Product.shippingCost(size:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "REQUIRE_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Product.shippingCost(size:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The argument 'scale' does not exist on field 'Product.dimension'.",
                            "The required argument 'unit' on field 'Product.dimension' was not provided."
                        ]
                    }
                }
                """
            ]);
    }

    // Referencing a field that exists in both the current schema and another schema.
    [Fact]
    public void Validate_RequireInvalidFieldsReferenceToLocalAndRemoteField_Fails()
    {
        AssertInvalid(
            [
                """
                type Book {
                    id: ID!
                    size: Int
                    pages(pageSize: Int @require(field: "size")): Int
                }
                """,
                """
                type Book {
                    id: ID!
                    size: Int
                }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Book.pages(pageSize:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "REQUIRE_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Book.pages(pageSize:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The required field 'Book.size' must not be served by the requiring source schema 'A'."
                        ]
                    }
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_FirstLevelFieldIsExternalInRequiringSchema()
    {
        AssertValid(
        [
            """
            type Book {
                id: ID!
                size: Int @external
                pages(pageSize: Int @require(field: "size")): Int
            }
            """,
            """
            type Book {
                id: ID!
                size: Int
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_NestedFieldIsServedByRequiringSchema()
    {
        AssertValid(
        [
            """
            type Order {
                id: ID!
                price(discount: Int @require(field: "customer.discount")): Int
            }

            type Customer {
                id: ID!
                discount: Int
            }
            """,
            """
            type Order {
                id: ID!
                customer: Customer
            }

            type Customer {
                id: ID!
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_FirstLevelIntermediateIsExternalInRequiringSchema()
    {
        AssertValid(
        [
            """
            type Order {
                id: ID!
                customer: Customer @external
                price(discount: Int @require(field: "customer.discount")): Int
            }

            type Customer {
                id: ID!
                discount: Int
            }
            """,
            """
            type Order {
                id: ID!
                customer: Customer
            }

            type Customer {
                id: ID!
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_RequiringSchemaServesFirstLevelIntermediateAndNestedField()
    {
        AssertInvalid(
            [
                """
                type Order {
                    id: ID!
                    customer: Customer
                    price(discount: Int @require(field: "customer.discount")): Int
                }

                type Customer {
                    id: ID!
                    discount: Int
                }
                """,
                """
                type Customer {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Order.price(discount:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "REQUIRE_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Order.price(discount:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The required field 'Order.customer' must not be served by the requiring source schema 'A'."
                        ]
                    }
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_RequiringSchemaServesFirstLevelIntermediateOnly()
    {
        AssertInvalid(
            [
                """
                type Order {
                    id: ID!
                    customer: Customer
                    price(discount: Int @require(field: "customer.discount")): Int
                }

                type Customer {
                    id: ID!
                }
                """,
                """
                type Customer {
                    id: ID!
                    discount: Int
                }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Order.price(discount:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "REQUIRE_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Order.price(discount:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The required field 'Order.customer' must not be served by the requiring source schema 'A'."
                        ]
                    }
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_FirstLevelIntermediateIsServedByRequiringAndOtherSchema()
    {
        AssertInvalid(
            [
                """
                type Order {
                    id: ID!
                    customer: Customer
                    price(discount: Int @require(field: "customer.discount")): Int
                }

                type Customer {
                    id: ID!
                    discount: Int
                }
                """,
                """
                type Order {
                    id: ID!
                    customer: Customer
                }

                type Customer {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Order.price(discount:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "REQUIRE_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Order.price(discount:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The required field 'Order.customer' must not be served by the requiring source schema 'A'."
                        ]
                    }
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_ReportFirstLevelFieldsInSelectionOrder_When_RequiringSchemaServesSeveral()
    {
        AssertInvalid(
            [
                """
                type Product {
                    id: ID!
                    weight: Int
                    dimension: Dimension
                    shippingCost(
                        input: ShippingInput
                            @require(field: "{ weight: weight, size: dimension.size }")
                    ): Int
                }

                type Dimension {
                    size: Int
                }

                input ShippingInput {
                    weight: Int
                    size: Int
                }
                """,
                """
                type Product {
                    id: ID!
                    weight: Int
                    dimension: Dimension
                }

                type Dimension {
                    size: Int
                }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Product.shippingCost(input:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "REQUIRE_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Product.shippingCost(input:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The required field 'Product.weight' must not be served by the requiring source schema 'A'.",
                            "The required field 'Product.dimension' must not be served by the requiring source schema 'A'."
                        ]
                    }
                }
                """
            ]);
    }

    [Theory]
    [InlineData("a", "b")]
    [InlineData("A", "B")]
    [InlineData("orderService", "customerService")]
    public void Compose_Should_ReportRequiredField_When_RequiringSchemaServesIt(
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
                    itemBySku(sku: String!): Item @lookup @internal
                }

                type Item @key(fields: "sku") {
                    sku: String!
                    weight: Int @shareable
                    shippingCost(weight: Int @require(field: "weight")): Float
                }
                """),
            new SourceSchemaText(
                otherSchemaName,
                """
                type Query {
                    items: [Item]
                    itemBySku(sku: String!): Item @lookup @internal
                }

                type Item @key(fields: "sku") {
                    sku: String!
                    weight: Int @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(LogEntryCodes.RequireInvalidFields, entry.Code);
        Assert.Equal(
            [
                "The required field 'Item.weight' must not be served by the requiring source schema "
                + $"'{requiringSchemaName}'."
            ],
            (IEnumerable<string>)entry.Extensions["errors"]!);
    }

    [Theory]
    [InlineData("a", "b")]
    [InlineData("A", "B")]
    [InlineData("orderService", "customerService")]
    public void Compose_Should_Succeed_When_RequiredFieldIsExternalInRequiringSchema(
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
                    itemBySku(sku: String!): Item @lookup @internal
                }

                type Item @key(fields: "sku") {
                    sku: String!
                    weight: Int @external
                    shippingCost(weight: Int @require(field: "weight")): Float
                }
                """),
            new SourceSchemaText(
                otherSchemaName,
                """
                type Query {
                    items: [Item]
                    itemBySku(sku: String!): Item @lookup @internal
                }

                type Item @key(fields: "sku") {
                    sku: String!
                    weight: Int
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
    public void Compose_Should_ReportFirstLevelIntermediate_When_RequiringSchemaServesItAndNestedField()
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
                    customer: Customer
                    price(discount: Int @require(field: "customer.discount")): Int
                }

                type Customer {
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

                type Order {
                    id: ID! @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The @require directive on argument 'Order.price(discount:)' in schema 'a' specifies an invalid field selection against the composed schema.",
                "code": "REQUIRE_INVALID_FIELDS",
                "severity": "Error",
                "coordinate": "Order.price(discount:)",
                "member": "require",
                "schema": "a",
                "extensions": {
                    "errors": [
                        "The required field 'Order.customer' must not be served by the requiring source schema 'a'."
                    ]
                }
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_ReportFirstLevelIntermediate_When_RequiringSchemaServesItAndOtherSchemaServesNestedField()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "a",
                """
                type Query {
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
                """),
            new SourceSchemaText(
                "b",
                """
                type Query {
                    orders: [Order]
                    customerById(id: ID!): Customer @lookup @internal
                }

                type Order {
                    id: ID! @shareable
                }

                type Customer {
                    id: ID! @shareable
                    discount: Int
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The @require directive on argument 'Order.price(discount:)' in schema 'a' specifies an invalid field selection against the composed schema.",
                "code": "REQUIRE_INVALID_FIELDS",
                "severity": "Error",
                "coordinate": "Order.price(discount:)",
                "member": "require",
                "schema": "a",
                "extensions": {
                    "errors": [
                        "The required field 'Order.customer' must not be served by the requiring source schema 'a'."
                    ]
                }
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_ReportFirstLevelIntermediate_When_RequiringAndOtherSchemaServeIt()
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
                    customer: Customer @shareable
                    price(discount: Int @require(field: "customer.discount")): Int
                }

                type Customer {
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

                type Order {
                    id: ID! @shareable
                    customer: Customer @shareable
                }

                type Customer {
                    id: ID! @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The @require directive on argument 'Order.price(discount:)' in schema 'a' specifies an invalid field selection against the composed schema.",
                "code": "REQUIRE_INVALID_FIELDS",
                "severity": "Error",
                "coordinate": "Order.price(discount:)",
                "member": "require",
                "schema": "a",
                "extensions": {
                    "errors": [
                        "The required field 'Order.customer' must not be served by the requiring source schema 'a'."
                    ]
                }
            }
            """
        ]);
    }
}

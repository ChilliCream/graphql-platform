namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerOutputFieldTests : SourceSchemaMergerTestBase
{
    // Imagine two schemas with a "discountPercentage" field on a "Product" type that slightly
    // differ in return type.
    [Fact]
    public void Merge_OutputFields_MatchesSnapshot()
    {
        AssertMatches(
            [
                """"
                # Schema A
                type Product {
                    """
                    Computes a discount as a percentage of the product's list price.
                    """
                    discountPercentage(percent: Int = 10): Int!
                }
                """",
                """
                # Schema B
                type Product {
                    discountPercentage(percent: Int): Int
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                "Computes a discount as a percentage of the product's list price."
                discountPercentage(percent: Int = 10
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)): Int
                    @fusion__field(schema: A, sourceType: "Int!")
                    @fusion__field(schema: B)
            }
            """);
    }

    // If the argument is missing in one of the schemas, the composed field will not include that
    // argument.
    [Fact]
    public void Merge_OutputFieldsMissingArgument_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Product {
                    discountPercentage(percent: Int): Int
                }
                """,
                """
                # Schema B
                type Product {
                    discountPercentage: Int
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                discountPercentage: Int
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """);
    }

    // In case one argument is marked with @inaccessible, the composed field will not include that
    // argument. Note: The argument will be included as inaccessible in the execution schema.
    [Fact]
    public void Merge_OutputFieldsWithInaccessibleArgument_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Product {
                    discountPercentage(percent: Int): Int
                }
                """,
                """
                # Schema B
                type Product {
                    discountPercentage(percent: Int @inaccessible): Int
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                discountPercentage(percent: Int
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)
                    @fusion__inaccessible): Int
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """);
    }

    // In case a schema defines a requirement through the @require directive, the composed field
    // will not include that argument.
    [Fact]
    public void Merge_OutputFieldsWithRequire_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Product {
                    percent: Int
                    discountPercentage(percent: Int): Int
                }
                """,
                """
                # Schema B
                type Product {
                    discountPercentage(percent: Int @require(field: "percent")): Int
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                discountPercentage: Int
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__requires(schema: B, requirements: "percent", field: "discountPercentage(percent: Int): Int", map: [ "percent" ])
                percent: Int
                    @fusion__field(schema: A)
            }
            """);
    }

    // Any field marked with @internal is removed from consideration before merging begins. This
    // ensures that internal fields do not appear in the final composed schema and also do not
    // affect the merging process. Internal fields are intended for internal use only and are not
    // part of the composed schema and can collide in their definitions.
    [Fact]
    public void Merge_OutputFieldsWithOneInternal_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Product {
                    discountPercentage: Int
                }
                """,
                """
                # Schema B
                type Product {
                    discountPercentage: Int @internal
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                discountPercentage: Int
                    @fusion__field(schema: A)
            }
            """);
    }

    // In the case where all fields are marked with @internal, the field will not appear in the
    // composed schema.
    [Fact]
    public void Merge_OutputFieldsWithBothInternal_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Product {
                    discountPercentage: Int @internal
                    name: String!
                }
                """,
                """
                # Schema B
                type Product {
                    discountPercentage: Int @internal
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                name: String!
                    @fusion__field(schema: A)
            }
            """);
    }

    // If any of the fields is marked as @inaccessible, then the merged field is also marked as
    // @inaccessible in the execution schema.
    [Fact]
    public void Merge_InaccessibleOutputField_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Product {
                    discountPercentage: Int
                }
                """,
                """
                # Schema B
                type Product {
                    discountPercentage: Int @inaccessible
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                discountPercentage: Int
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @fusion__inaccessible
            }
            """);
    }

    // The field "price" is only available in Schema C, it is overridden elsewhere.
    [Fact]
    public void Merge_OutputFieldsWithOverrides_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                type Product @key(fields: "id") {
                    id: ID!
                    name: String!
                    price: Float!
                }
                """,
                """
                type Product @key(fields: "id") {
                    id: ID! @external
                    price: Float! @override(from: "A")
                    tax: Float!
                }
                """,
                """
                type Product @key(fields: "id") {
                    id: ID! @external
                    price: Float! @override(from: "B")
                    tax: Float!
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B)
                @fusion__type(schema: C) {
                id: ID!
                    @fusion__field(schema: A)
                    @fusion__field(schema: B, partial: true)
                    @fusion__field(schema: C, partial: true)
                name: String!
                    @fusion__field(schema: A)
                price: Float!
                    @fusion__field(schema: C)
                tax: Float!
                    @fusion__field(schema: B)
                    @fusion__field(schema: C)
            }
            """);
    }

    // @provides/@external
    [Fact]
    public void Merge_OutputFieldWithProvidesAndExternal_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                type Review {
                    id: ID!
                    body: String!
                    author: User @provides(fields: "email")
                }

                type User @key(fields: "id") {
                    id: ID!
                    email: String! @external
                    name: String!
                }

                type Query {
                    reviews: [Review!]
                    users: [User!]
                }
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A) {
                reviews: [Review!]
                    @fusion__field(schema: A)
                users: [User!]
                    @fusion__field(schema: A)
            }

            type Review
                @fusion__type(schema: A) {
                author: User
                    @fusion__field(schema: A, provides: "email")
                body: String!
                    @fusion__field(schema: A)
                id: ID!
                    @fusion__field(schema: A)
            }

            type User
                @fusion__type(schema: A) {
                email: String!
                    @fusion__field(schema: A, partial: true)
                id: ID!
                    @fusion__field(schema: A)
                name: String!
                    @fusion__field(schema: A)
            }
            """);
    }

    // Even if an output field is only @deprecated in one source schema, the composite output field
    // is marked as @deprecated.
    [Fact]
    public void Merge_DeprecatedOutputField_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Product {
                    name: String @deprecated(reason: "Some reason")
                }
                """,
                """
                # Schema B
                type Product {
                    name: String
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                name: String
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @deprecated(reason: "Some reason")
            }
            """);
    }

    // If the same output field is @deprecated in multiple source schemas, the first non-null
    // deprecation reason is chosen.
    [Fact]
    public void Merge_DeprecatedOutputFieldsUsesFirstNonNullReason_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Product {
                    name: String @deprecated(reason: "Some reason")
                }
                """,
                """
                # Schema B
                type Product {
                    name: String @deprecated(reason: "Another reason")
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                name: String
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @deprecated(reason: "Some reason")
            }
            """);
    }

    // If an output field is deprecated without a deprecation reason, a default reason is inserted
    // to be compatible with the latest spec.
    [Fact]
    public void Merge_DeprecatedOutputFieldsWithoutReasonInsertsDefaultReason_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Product {
                    name: String @deprecated
                }
                """,
                """
                # Schema B
                type Product {
                    name: String @deprecated
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                name: String
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                    @deprecated(reason: "No longer supported.")
            }
            """);
    }
}

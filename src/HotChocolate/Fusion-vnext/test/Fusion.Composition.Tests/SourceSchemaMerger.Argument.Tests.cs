namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerArgumentTests : SourceSchemaMergerTestBase
{
    // Consider two field definitions that share the same "filter" argument, but with slightly
    // different types and descriptions. In the merged schema, the "filter" argument is defined with
    // the most restrictive type ("ProductFilter!"), includes the description from the original
    // field in Schema A, and is marked as required.
    [Fact]
    public void Merge_Arguments_MatchesSnapshot()
    {
        AssertMatches(
            [
                """"
                # Schema A
                type Query {
                    searchProducts(
                        """
                        Filter to apply to the search
                        """
                        filter: ProductFilter!
                    ): [Product]
                }
                """",
                """"
                # Schema B
                type Query {
                    searchProducts(
                        """
                        Search filter to apply
                        """
                        filter: ProductFilter
                    ): [Product]
                }
                """"
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                searchProducts("Filter to apply to the search" filter: ProductFilter!
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B, sourceType: "ProductFilter")): [Product]
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            scalar Product
                @fusion__type(schema: A)
                @fusion__type(schema: B)

            scalar ProductFilter
                @fusion__type(schema: A)
                @fusion__type(schema: B)
            """);
    }

    // If any of the arguments is marked as @inaccessible, then the merged argument is also marked
    // as @inaccessible in the execution schema.
    [Fact]
    public void Merge_InaccessibleArgument_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    field(limit: Int): Int
                }
                """,
                """
                # Schema B
                type Query {
                    field(limit: Int @inaccessible): Int
                }
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field(limit: Int
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)
                    @fusion__inaccessible): Int
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """);
    }

    // Suppose we have two variants of the same argument, "limit", from different services.
    [Fact]
    public void Merge_ArgumentsDifferentNullabilityAndDefault_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    field(limit: Int = 10): Int
                }
                """,
                """"
                # Schema B
                type Query {
                    field(
                        """
                        Number of items to fetch
                        """
                        limit: Int!
                    ): Int
                }
                """"
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field("Number of items to fetch" limit: Int! = 10
                    @fusion__inputField(schema: A, sourceType: "Int")
                    @fusion__inputField(schema: B)): Int
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """);
    }

    // @require
    [Fact]
    public void Merge_ArgumentsWithRequire_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                type Product {
                    id: ID!
                    dimension: ProductDimension!
                    delivery(
                        zip: String!
                        size: Int! @require(field: "dimension.size")
                        weight: Int! @require(field: "dimension.weight")
                    ): DeliveryEstimates
                }

                type ProductDimension {
                    size: Int!
                    weight: Int!
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A) {
                delivery(zip: String!
                    @fusion__inputField(schema: A)): DeliveryEstimates
                    @fusion__field(schema: A)
                    @fusion__requires(schema: A, requirements: "dimension { size weight }", field: "delivery(zip: String! size: Int! weight: Int!): DeliveryEstimates", map: [ null, "dimension.size", "dimension.weight" ])
                dimension: ProductDimension!
                    @fusion__field(schema: A)
                id: ID!
                    @fusion__field(schema: A)
            }

            type ProductDimension
                @fusion__type(schema: A) {
                size: Int!
                    @fusion__field(schema: A)
                weight: Int!
                    @fusion__field(schema: A)
            }

            scalar DeliveryEstimates
                @fusion__type(schema: A)
            """);
    }

    // Even if an argument is only @deprecated in one source schema, the composite argument is
    // marked as @deprecated.
    [Fact]
    public void Merge_DeprecatedArgument_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Product {
                    reviews(filter: String @deprecated(reason: "Some reason")): [String]
                }
                """,
                """
                # Schema B
                type Product {
                    reviews(filter: String): [String]
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                reviews(filter: String
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)
                    @deprecated(reason: "Some reason")): [String]
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """);
    }

    // If the same argument is @deprecated in multiple source schemas, the first non-null
    // deprecation reason is chosen.
    [Fact]
    public void Merge_DeprecatedArgumentsUsesFirstNonNullReason_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Product {
                    reviews(filter: String @deprecated(reason: "Some reason")): [String]
                }
                """,
                """
                # Schema B
                type Product {
                    reviews(filter: String @deprecated(reason: "Another reason")): [String]
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                reviews(filter: String
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)
                    @deprecated(reason: "Some reason")): [String]
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """);
    }

    // If an argument is deprecated without a deprecation reason, a default reason is inserted to be
    // compatible with the latest spec.
    [Fact]
    public void Merge_DeprecatedArgumentsWithoutReasonInsertsDefaultReason_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Product {
                    reviews(filter: String @deprecated): [String]
                }
                """,
                """
                # Schema B
                type Product {
                    reviews(filter: String @deprecated): [String]
                }
                """
            ],
            """
            type Product
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                reviews(filter: String
                    @fusion__inputField(schema: A)
                    @fusion__inputField(schema: B)
                    @deprecated(reason: "No longer supported.")): [String]
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """);
    }
}

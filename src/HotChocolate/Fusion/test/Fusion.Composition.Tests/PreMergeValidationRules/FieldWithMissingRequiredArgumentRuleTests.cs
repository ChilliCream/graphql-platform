namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class FieldWithMissingRequiredArgumentRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new FieldWithMissingRequiredArgumentRule();

    // All schemas agree on having a required argument "author" for the "books" field.
    [Fact]
    public void Validate_FieldWithoutMissingRequiredArgument_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Query {
                books(author: String!): [Book] @shareable
            }
            """,
            """
            # Schema B
            type Query {
                books(author: String!): [Book] @shareable
            }
            """
        ]);
    }

    // In the following example, the "author" argument on the "books" field in Schema A specifies a
    // dependency on the "author" field in Schema C. The "author" argument on the books field in
    // Schema B is optional. As a result, the composition succeeds; however, the "author" argument
    // will not be included in the composite schema.
    [Fact]
    public void Validate_FieldWithOptionalArgument_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Collection {
                books(author: String! @require(field: "author")): [Book] @shareable
            }
            """,
            """
            # Schema B
            type Collection {
                books(author: String): [Book] @shareable
            }
            """,
            """
            # Schema C
            type Collection {
                author: String!
            }
            """
        ]);
    }

    // In the following example, the "author" argument is required in one schema but not in the
    // other. This will result in a FIELD_WITH_MISSING_REQUIRED_ARGUMENT error.
    [Fact]
    public void Validate_FieldWithMissingRequiredArgument_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    books(author: String!): [Book] @shareable
                }
                """,
                """
                # Schema B
                type Query {
                    books: [Book] @shareable
                }
                """
            ],
            [
                """
                {
                    "message": "The argument 'Query.books(author:)' must be defined as required in schema 'B'. Arguments marked with @require are treated as non-required.",
                    "code": "FIELD_WITH_MISSING_REQUIRED_ARGUMENT",
                    "severity": "Error",
                    "coordinate": "Query.books(author:)",
                    "member": "books",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }

    // In the following example, the "author" argument on the "books" field in Schema A specifies a
    // dependency on the "author" field in Schema C. The "author" argument on the "books" field in
    // Schema B is not optional. This will result in a FIELD_WITH_MISSING_REQUIRED_ARGUMENT error.
    [Fact]
    public void Validate_FieldWithRequiredArgument_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Collection {
                    books(author: String! @require(field: "author")): [Book] @shareable
                }
                """,
                """
                # Schema B
                type Collection {
                    books(author: String!): [Book] @shareable
                }
                """,
                """
                # Schema C
                type Collection {
                    author: String!
                }
                """
            ],
            [
                """
                {
                    "message": "The argument 'Collection.books(author:)' must be defined as required in schema 'A'. Arguments marked with @require are treated as non-required.",
                    "code": "FIELD_WITH_MISSING_REQUIRED_ARGUMENT",
                    "severity": "Error",
                    "coordinate": "Collection.books(author:)",
                    "member": "books",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // The same rule applies when the contributing schemas are @interfaceObject stand-ins for the
    // same interface. Schema B supplies "minRating" from the executor via @require, while schema C
    // declares it as a client-supplied non-null argument. The two are not mergeable.
    [Fact]
    public void Validate_InterfaceObjectStandInWithRequiredArgument_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                interface Media {
                    id: ID!
                    title: String!
                    rating: Int!
                }
                """,
                """
                # Schema B
                type Media @interfaceObject @key(fields: "id") {
                    id: ID!
                    recommended(minRating: Int! @require(field: "rating")): [Review!]! @shareable
                }

                type Review {
                    id: ID! @shareable
                    rating: Int! @shareable
                }
                """,
                """
                # Schema C
                type Media @interfaceObject @key(fields: "id") {
                    id: ID!
                    recommended(minRating: Int!): [Review!]! @shareable
                }

                type Review {
                    id: ID! @shareable
                    rating: Int! @shareable
                }
                """
            ],
            [
                """
                {
                    "message": "The argument 'Media.recommended(minRating:)' must be defined as required in schema 'B'. Arguments marked with @require are treated as non-required.",
                    "code": "FIELD_WITH_MISSING_REQUIRED_ARGUMENT",
                    "severity": "Error",
                    "coordinate": "Media.recommended(minRating:)",
                    "member": "recommended",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}

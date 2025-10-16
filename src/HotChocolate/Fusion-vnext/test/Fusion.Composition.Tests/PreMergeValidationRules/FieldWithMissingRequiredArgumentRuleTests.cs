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
                "The argument 'Query.books(author:)' must be defined as required in schema 'B'. "
                + "Arguments marked with @require are treated as non-required."
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
                "The argument 'Collection.books(author:)' must be defined as required in schema "
                + "'A'. Arguments marked with @require are treated as non-required."
            ]);
    }
}

namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class FieldArgumentNotSharedRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new FieldArgumentNotSharedRule();

    // If all schemas define the same arguments on "Query.books", no argument is removed.
    [Fact]
    public void Validate_FieldArgumentsShared_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Query {
                books(category: String, limit: Int): [Book] @shareable
            }

            type Book {
                id: ID!
            }
            """,
            """
            # Schema B
            type Query {
                books(category: String, limit: Int): [Book] @shareable
            }

            type Book {
                id: ID!
            }
            """
        ]);
    }

    // Required and inaccessible arguments that are not shared are not reported by this rule.
    [Fact]
    public void Validate_RequiredOrInaccessibleFieldArgumentNotShared_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Query {
                books(category: String!, limit: Int @inaccessible): [Book] @shareable
            }

            type Book {
                id: ID!
            }
            """,
            """
            # Schema B
            type Query {
                books: [Book] @shareable
            }

            type Book {
                id: ID!
            }
            """
        ]);
    }

    // A field defined in only one schema keeps all of its arguments.
    [Fact]
    public void Validate_FieldDefinedInOneSchema_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Query {
                books(category: String): [Book]
            }

            type Book {
                id: ID!
            }
            """,
            """
            # Schema B
            type Query {
                authors: [String]
            }
            """
        ]);
    }

    // The optional argument "limit" is defined in schema A only, and the argument "category" is
    // marked with @require in schema B, so both are removed from the composite schema.
    [Fact]
    public void Validate_OptionalFieldArgumentNotShared_Warns()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    books(category: String, limit: Int): [Book] @shareable
                }

                type Book {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    books(category: String @require(field: "id")): [Book] @shareable
                }

                type Book {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The argument 'Query.books(category:)' is not defined in schema 'B' and is removed from the composite schema.",
                    "code": "FIELD_ARGUMENT_NOT_SHARED",
                    "severity": "Warning",
                    "coordinate": "Query.books(category:)",
                    "member": "books",
                    "schema": "B",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "The argument 'Query.books(limit:)' is not defined in schema 'B' and is removed from the composite schema.",
                    "code": "FIELD_ARGUMENT_NOT_SHARED",
                    "severity": "Warning",
                    "coordinate": "Query.books(limit:)",
                    "member": "books",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}

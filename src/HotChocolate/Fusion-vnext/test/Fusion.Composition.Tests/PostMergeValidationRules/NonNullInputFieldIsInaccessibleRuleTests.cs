namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class NonNullInputFieldIsInaccessibleRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new NonNullInputFieldIsInaccessibleRule();

    // The following is valid because the "age" field, although @inaccessible in one source schema,
    // is nullable and can be safely omitted in the final schema without breaking any mandatory
    // input requirement.
    [Fact]
    public void Validate_NullInputFieldIsInaccessible_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter {
                author: String!
                age: Int @inaccessible
            }
            """,
            """
            input BookFilter {
                author: String!
                age: Int
            }
            """
        ]);
    }

    // Another valid case is when a nullable input field is removed during merging.
    [Fact]
    public void Validate_NullableInputFieldRemovedDuringMerging_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter {
                author: String!
                age: Int
            }
            """,
            """
            # Schema B
            input BookFilter {
                author: String!
            }
            """
        ]);
    }

    // An invalid case is when a non-null input field is inaccessible.
    [Fact]
    public void Validate_NonNullInputFieldIsInaccessible_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                input BookFilter {
                    author: String!
                    age: Int!
                }
                """,
                """
                # Schema B
                input BookFilter {
                    author: String!
                    age: Int @inaccessible
                }
                """
            ],
            [
                "The non-null input field 'BookFilter.age' in schema 'A' must be accessible in the "
                + "composed schema."
            ]);
    }

    // Another invalid case is when a non-null input field is removed during merging.
    [Fact]
    public void Validate_NonNullInputFieldRemovedDuringMerging_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                input BookFilter {
                    author: String!
                    age: Int!
                }
                """,
                """
                input BookFilter {
                    author: String!
                }
                """
            ],
            [
                "The non-null input field 'BookFilter.age' in schema 'A' must be accessible in the "
                + "composed schema."
            ]);
    }
}

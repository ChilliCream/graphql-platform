namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class InputFieldTypesMergeableRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InputFieldTypesMergeableRule();

    // In this example, the field "name" in "AuthorInput" has compatible types across source
    // schemas, making them mergeable.
    [Fact]
    public void Validate_InputFieldTypesMergeable_Succeeds()
    {
        AssertValid(
        [
            """
            input AuthorInput {
                name: String!
            }
            """,
            """
            input AuthorInput {
                name: String
            }
            """
        ]);
    }

    // The following example shows that fields are mergeable if they have different nullability but
    // the named type is the same and the list structure is the same.
    [Fact]
    public void Validate_InputFieldTypesMergeableDifferentNullability_Succeeds()
    {
        AssertValid(
        [
            """
            input AuthorInput {
                tags: [String!]
            }
            """,
            """
            input AuthorInput {
                tags: [String]!
            }
            """,
            """
            input AuthorInput {
                tags: [String]
            }
            """
        ]);
    }

    // Multiple input fields.
    [Fact]
    public void Validate_InputFieldTypesMergeableMultipleInputFields_Succeeds()
    {
        AssertValid(
        [
            """
            input AuthorInput {
                name: String!
                tags: [String!]
                birthdate: DateTime
            }
            """,
            """
            input AuthorInput {
                name: String
                tags: [String]!
                birthdate: DateTime!
            }
            """
        ]);
    }

    // In this example, the field "birthdate" on "AuthorInput" is not mergeable as the field has
    // different named types ("String" and "DateTime") across source schemas.
    [Fact]
    public void Validate_InputFieldTypesNotMergeableDifferentNamedTypes_Fails()
    {
        AssertInvalid(
            [
                """
                input AuthorInput {
                    birthdate: String!
                }
                """,
                """
                input AuthorInput {
                    birthdate: DateTime!
                }
                """
            ],
            [
                "The input field 'AuthorInput.birthdate' has a different type shape in schema 'A' "
                + "than it does in schema 'B'."
            ]);
    }

    // List versus non-list.
    [Fact]
    public void Validate_InputFieldTypesNotMergeableListVsNonList_Fails()
    {
        AssertInvalid(
            [
                """
                input AuthorInput {
                    birthdate: String!
                }
                """,
                """
                input AuthorInput {
                    birthdate: [String!]
                }
                """
            ],
            [
                "The input field 'AuthorInput.birthdate' has a different type shape in schema 'A' "
                + "than it does in schema 'B'."
            ]);
    }
}

namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class InputWithMissingRequiredFieldsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InputWithMissingRequiredFieldsRule();

    // If all schemas define "BookFilter" with the required field "title", the rule is satisfied.
    [Fact]
    public void Validate_InputWithoutMissingRequiredFields_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter {
                title: String!
                author: String
            }
            """,
            """
            # Schema B
            input BookFilter {
                title: String!
                yearPublished: Int
            }
            """
        ]);
    }

    // Multiple required input fields.
    [Fact]
    public void Validate_InputWithoutMissingRequiredFieldsMultipleRequiredFields_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter {
                title: String!
                author: String!
            }
            """,
            """
            # Schema B
            input BookFilter {
                title: String!
                author: String!
                yearPublished: Int
            }
            """
        ]);
    }

    // If "title" is required in one source schema but missing in another, this violates the rule.
    // In this case, "title" is mandatory in "Schema A" but not defined in "Schema B", causing
    // inconsistency in required fields across schemas.
    [Fact]
    public void Validate_InputWithMissingRequiredFields_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                input BookFilter {
                    title: String!
                    author: String
                }
                """,
                """
                # Schema B
                input BookFilter {
                    author: String
                    yearPublished: Int
                }
                """
            ],
            [
                "The input type 'BookFilter' in schema 'B' must define the required field 'title'."
            ]);
    }

    // Multiple required input fields.
    [Fact]
    public void Validate_InputWithMissingRequiredFieldsMultipleRequiredFields_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                input BookFilter {
                    title: String!
                    yearPublished: Int
                }
                """,
                """
                # Schema B
                input BookFilter {
                    author: String!
                    yearPublished: Int
                }
                """
            ],
            [
                "The input type 'BookFilter' in schema 'A' must define the required field "
                + "'author'.",

                "The input type 'BookFilter' in schema 'B' must define the required field 'title'."
            ]);
    }
}

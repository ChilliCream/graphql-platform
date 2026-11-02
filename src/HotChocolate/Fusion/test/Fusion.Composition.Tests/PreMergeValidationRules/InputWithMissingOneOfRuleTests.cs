namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class InputWithMissingOneOfRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InputWithMissingOneOfRule();

    // If all schemas define "BookFilter" as @oneOf, the rule is satisfied.
    [Fact]
    public void Validate_All_Inputs_With_OneOf_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter @oneOf {
                title: String
                author: String
            }
            """,
            """
            # Schema B
            input BookFilter @oneOf {
                title: String
                author: String
            }
            """
        ]);
    }

    // If all schemas define "BookFilter" without @oneOf, the rule is satisfied.
    [Fact]
    public void Validate_All_Inputs_Without_OneOf_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter {
                title: String
                author: String
            }
            """,
            """
            # Schema B
            input BookFilter {
                title: String
                author: String
            }
            """
        ]);
    }

    // If "BookFilter" does not contain @oneOf in one schema, but does in the others,
    // the rule is not satisfied.
    [Fact]
    public void Validate_Input_With_Missing_OneOf_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                input BookFilter @oneOf {
                    title: String
                    author: String
                }
                """,
                """
                # Schema B
                input BookFilter {
                    title: String
                    author: String
                }
                """
            ],
            [
                """
                {
                  "message": "The input type 'BookFilter' in schema 'B' must be annotated with the '@oneOf` directive, since the same type has been annotated with it in schema 'A'.",
                  "code": "INPUT_WITH_MISSING_ONEOF",
                  "severity": "Error",
                  "coordinate": "BookFilter",
                  "member": "BookFilter",
                  "schema": "B",
                  "extensions": {}
                }
                """
            ]);
    }

    // If "BookFilter" does not contain @oneOf in one schema, but does in the others,
    // the rule is not satisfied.
    [Fact]
    public void Validate_Input_With_Missing_OneOf_OneOf_Comes_Last_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                input BookFilter {
                    title: String
                    author: String
                }
                """,
                """
                # Schema B
                input BookFilter @oneOf {
                    title: String
                    author: String
                }
                """
            ],
            [
                """
                {
                  "message": "The input type 'BookFilter' in schema 'A' must be annotated with the '@oneOf` directive, since the same type has been annotated with it in schema 'B'.",
                  "code": "INPUT_WITH_MISSING_ONEOF",
                  "severity": "Error",
                  "coordinate": "BookFilter",
                  "member": "BookFilter",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }
}

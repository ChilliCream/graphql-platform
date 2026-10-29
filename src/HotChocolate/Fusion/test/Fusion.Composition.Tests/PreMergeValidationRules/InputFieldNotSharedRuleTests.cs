namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class InputFieldNotSharedRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InputFieldNotSharedRule();

    // If all schemas define the same fields on "BookFilter", no field is removed.
    [Fact]
    public void Validate_InputFieldsShared_Succeeds()
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

    // Required and inaccessible fields that are not shared are not reported by this rule.
    [Fact]
    public void Validate_RequiredOrInaccessibleInputFieldNotShared_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter {
                title: String!
                author: String @inaccessible
            }
            """,
            """
            # Schema B
            input BookFilter {
                yearPublished: Int @inaccessible
            }
            """
        ]);
    }

    // An inaccessible input type is not part of the composite schema.
    [Fact]
    public void Validate_InaccessibleInputTypeWithFieldNotShared_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter @inaccessible {
                title: String
                author: String
            }
            """,
            """
            # Schema B
            input BookFilter {
                title: String
            }
            """
        ]);
    }

    // The optional fields "author" and "yearPublished" are each defined in only one schema, so
    // they are removed from the composite schema.
    [Fact]
    public void Validate_OptionalInputFieldNotShared_Warns()
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
                input BookFilter {
                    title: String
                    yearPublished: Int
                }
                """
            ],
            [
                """
                {
                    "message": "The input field 'BookFilter.yearPublished' is not defined in schema 'A' and is removed from the composite schema.",
                    "code": "INPUT_FIELD_NOT_SHARED",
                    "severity": "Warning",
                    "coordinate": "BookFilter.yearPublished",
                    "member": "BookFilter",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "The input field 'BookFilter.author' is not defined in schema 'B' and is removed from the composite schema.",
                    "code": "INPUT_FIELD_NOT_SHARED",
                    "severity": "Warning",
                    "coordinate": "BookFilter.author",
                    "member": "BookFilter",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}

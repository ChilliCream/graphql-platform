using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class DirectiveExtensionTargetIsDefinedRuleTests
    : RuleTestBase<DirectiveExtensionTargetIsDefinedRule>
{
    [Fact]
    public void Validate_ExtensionOfDefinedDirective_Succeeds()
    {
        AssertValid(
            """
            directive @meta on DIRECTIVE_DEFINITION

            directive @foo on OBJECT

            extend directive @foo @meta
            """);
    }

    [Fact]
    public void Validate_ExtensionOfUndefinedDirective_Fails()
    {
        AssertInvalid(
            """
            directive @meta on DIRECTIVE_DEFINITION

            extend directive @missing @meta
            """,
            """
            {
                "message": "The directive extension 'extend directive @missing' targets an undefined directive.",
                "code": "HCV0034",
                "severity": "Error",
                "coordinate": "@missing",
                "member": "@missing",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2026/#sec-Directive-Extensions.Type-Validation"
                }
            }
            """);
    }
}

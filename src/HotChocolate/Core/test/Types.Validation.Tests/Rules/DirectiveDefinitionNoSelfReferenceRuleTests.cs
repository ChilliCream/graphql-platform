using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class DirectiveDefinitionNoSelfReferenceRuleTests
    : RuleTestBase<DirectiveDefinitionNoSelfReferenceRule>
{
    [Fact]
    public void Validate_DirectiveDoesNotReferenceItself_Succeeds()
    {
        AssertValid(
            """
            directive @onDirectiveDefinition on DIRECTIVE_DEFINITION

            directive @custom @onDirectiveDefinition on OBJECT
            """);
    }

    [Fact]
    public void Validate_DirectiveDefinitionSelfApplication_Fails()
    {
        AssertInvalid(
            """
            directive @custom @custom on DIRECTIVE_DEFINITION
            """,
            """
            {
                "message": "The directive definition '@custom' must not reference itself.",
                "code": "HCV0027",
                "severity": "Error",
                "coordinate": "@custom",
                "member": "@custom",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveDefinitionArgumentSelfApplication_Fails()
    {
        AssertInvalid(
            """
            directive @custom(arg: Int @custom) on ARGUMENT_DEFINITION | DIRECTIVE_DEFINITION
            """,
            """
            {
                "message": "The directive definition '@custom' must not reference itself.",
                "code": "HCV0027",
                "severity": "Error",
                "coordinate": "@custom",
                "member": "@custom",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }
}

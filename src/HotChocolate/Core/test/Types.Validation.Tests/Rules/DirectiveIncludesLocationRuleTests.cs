using HotChocolate.Rules;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Types.Validation.Rules;

public sealed class DirectiveIncludesLocationRuleTests : RuleTestBase<DirectiveIncludesLocationRule>
{
    [Fact]
    public void Validate_DirectiveWithLocation_Succeeds()
    {
        AssertValid("directive @foo on OBJECT");
    }

    [Fact]
    public void Validate_DirectiveWithoutLocation_Fails()
    {
        AssertInvalid(
            new MutableSchemaDefinition
            {
                DirectiveDefinitions = { new MutableDirectiveDefinition("foo") }
            },
            """
            {
                "message": "The Directive definition '@foo' must include at least one DirectiveLocation.",
                "code": "HCV0020",
                "severity": "Error",
                "coordinate": "@foo",
                "member": "@foo",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }
}

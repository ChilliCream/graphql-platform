using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class NonEmptyInterfaceTypeRuleTests : RuleTestBase<NonEmptyInterfaceTypeRule>
{
    [Fact]
    public void Validate_NonEmptyInterfaceType_Succeeds()
    {
        AssertValid(
            """
            interface Foo {
                id: ID!
            }
            """);
    }

    [Fact]
    public void Validate_EmptyInterfaceType_Fails()
    {
        AssertInvalid(
            "interface Foo",
            """
            {
                "message": "The Interface type 'Foo' must define one or more fields.",
                "code": "HCV0012",
                "severity": "Error",
                "coordinate": "Foo",
                "member": "Foo",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Interfaces.Type-Validation"
                }
            }
            """);
    }
}

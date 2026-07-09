using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class NonEmptyInputObjectTypeRuleTests : RuleTestBase<NonEmptyInputObjectTypeRule>
{
    [Fact]
    public void Validate_NonEmptyInputObjectType_Succeeds()
    {
        AssertValid(
            """
            input Foo {
                id: ID!
            }
            """);
    }

    [Fact]
    public void Validate_EmptyInputObjectType_Fails()
    {
        AssertInvalid(
            "input Foo",
            """
            {
                "message": "The Input Object type 'Foo' must define one or more fields.",
                "code": "HCV0016",
                "severity": "Error",
                "coordinate": "Foo",
                "member": "Foo",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """);
    }
}

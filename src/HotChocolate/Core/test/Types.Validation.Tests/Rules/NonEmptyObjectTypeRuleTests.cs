using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class NonEmptyObjectTypeRuleTests : RuleTestBase<NonEmptyObjectTypeRule>
{
    [Fact]
    public void Validate_NonEmptyObjectType_Succeeds()
    {
        AssertValid(
            """
            type Foo {
                id: ID!
            }
            """);
    }

    [Fact]
    public void Validate_EmptyObjectType_Fails()
    {
        AssertInvalid(
            "type Foo",
            """
            {
                "message": "The Object type 'Foo' must define one or more fields.",
                "code": "HCV0001",
                "severity": "Error",
                "coordinate": "Foo",
                "member": "Foo",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """);
    }
}

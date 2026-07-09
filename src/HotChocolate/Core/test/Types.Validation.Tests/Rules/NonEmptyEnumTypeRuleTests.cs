using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class NonEmptyEnumTypeRuleTests : RuleTestBase<NonEmptyEnumTypeRule>
{
    [Fact]
    public void Validate_NonEmptyEnumType_Succeeds()
    {
        AssertValid(
            """
            enum Foo {
                VALUE
            }
            """);
    }

    [Fact]
    public void Validate_EmptyEnumType_Fails()
    {
        AssertInvalid(
            "enum Foo",
            """
            {
                "message": "The Enum type 'Foo' must define one or more values.",
                "code": "HCV0015",
                "severity": "Error",
                "coordinate": "Foo",
                "member": "Foo",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Enums.Type-Validation"
                }
            }
            """);
    }
}

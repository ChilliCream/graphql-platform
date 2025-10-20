using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class NonEmptyUnionTypeRuleTests : RuleTestBase<NonEmptyUnionTypeRule>
{
    [Fact]
    public void Validate_NonEmptyUnionType_Succeeds()
    {
        AssertValid(
            """
            union FooUnion = FooObject
            type FooObject
            """);
    }

    [Fact]
    public void Validate_EmptyUnionType_Fails()
    {
        AssertInvalid(
            "union FooUnion",
            """
            {
                "message": "The Union type 'FooUnion' must define one or more member types.",
                "code": "HCV0014",
                "severity": "Error",
                "coordinate": "FooUnion",
                "member": "FooUnion",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Unions.Type-Validation"
                }
            }
            """);
    }
}

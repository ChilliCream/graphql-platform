using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class ValidObjectDeprecationRuleTests : RuleTestBase<ValidObjectDeprecationRule>
{
    [Fact]
    public void Validate_Should_Succeed_When_DeprecatedObjectIsUnionMember()
    {
        AssertValid(
            """
            type Query { animals: Animal }

            union Animal = Dog | Dragon

            type Dog { name: String }

            type Dragon @deprecated(reason: "No longer known to exist.") { name: String }
            """);
    }

    [Fact]
    public void Validate_Should_Succeed_When_DeprecatedObjectImplementsInterface()
    {
        AssertValid(
            """
            type Query { animals: [Animal] }

            interface Animal { name: String }

            type Dog implements Animal { name: String }

            type Dragon implements Animal @deprecated(reason: "Gone.") { name: String }
            """);
    }

    [Fact]
    public void Validate_Should_Succeed_When_DeprecatedFieldReturnsDeprecatedObject()
    {
        AssertValid(
            """
            type Query { dragon: Dragon @deprecated(reason: "Use dog.") }

            type Dragon @deprecated(reason: "Gone.") { name: String }
            """);
    }

    [Fact]
    public void Validate_Should_Fail_When_NonDeprecatedFieldReturnsDeprecatedObject()
    {
        AssertInvalid(
            """
            type Query { dragon: Dragon }

            type Dragon @deprecated(reason: "Gone.") { name: String }
            """,
            """
            {
                "message": "The type of 'Query.dragon' is the deprecated type 'Dragon'. Either deprecate the field or change its return type.",
                "code": "HCV0030",
                "severity": "Error",
                "coordinate": "Query.dragon",
                "member": "dragon",
                "extensions": {}
            }
            """);
    }

    [Fact]
    public void Validate_Should_Fail_When_NonDeprecatedFieldReturnsWrappedDeprecatedObject()
    {
        AssertInvalid(
            """
            type Query { dragons: [Dragon!]! }

            type Dragon @deprecated(reason: "Gone.") { name: String }
            """,
            """
            {
                "message": "The type of 'Query.dragons' is the deprecated type 'Dragon'. Either deprecate the field or change its return type.",
                "code": "HCV0030",
                "severity": "Error",
                "coordinate": "Query.dragons",
                "member": "dragons",
                "extensions": {}
            }
            """);
    }
}

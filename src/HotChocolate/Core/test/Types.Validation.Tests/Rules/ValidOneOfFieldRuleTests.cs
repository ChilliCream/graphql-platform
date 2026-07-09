using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class ValidOneOfFieldRuleTests : RuleTestBase<ValidOneOfFieldRule>
{
    [Fact]
    public void Validate_ValidOneOfField_Succeeds()
    {
        AssertValid(
            """
            input Foo @oneOf {
                field1: Int
                field2: Int
            }
            """);
    }

    [Fact]
    public void Validate_InvalidOneOfFieldNonNullable_Fails()
    {
        AssertInvalid(
            """
            input Foo @oneOf {
                field1: Int!
                field2: Int
            }
            """,
            """
            {
                "message": "The OneOf Input Object field 'field1' must be nullable and must not have a default value.",
                "code": "HCV0017",
                "severity": "Error",
                "coordinate": "Foo.field1",
                "member": "field1",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_InvalidOneOfFieldWithDefault_Fails()
    {
        AssertInvalid(
            """
            input Foo @oneOf {
                field1: Int = 1
                field2: Int
            }
            """,
            """
            {
                "message": "The OneOf Input Object field 'field1' must be nullable and must not have a default value.",
                "code": "HCV0017",
                "severity": "Error",
                "coordinate": "Foo.field1",
                "member": "field1",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """);
    }
}

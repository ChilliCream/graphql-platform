using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class NoSelfImplementationRuleTests : RuleTestBase<NoSelfImplementationRule>
{
    [Fact]
    public void Validate_NoSelfImplementation_Succeeds()
    {
        AssertValid(
            """
            interface Foo {
                field1: Int
            }

            interface Bar {
                field2: Int
            }

            interface Baz implements Foo & Bar {
                field1: Int
                field2: Int
            }
            """);
    }

    [Fact]
    public void Validate_SelfImplementation_Fails()
    {
        AssertInvalid(
            """
            interface Foo implements Foo {
                field: Int
            }
            """,
            """
            {
                "message": "The Interface type 'Foo' may not implement itself.",
                "code": "HCV0013",
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

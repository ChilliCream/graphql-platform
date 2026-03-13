using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class NoInputObjectDefaultValueCycleRuleTests : RuleTestBase<NoInputObjectDefaultValueCycleRule>
{
    [Fact]
    public void Validate_NoInputObjectDefaultValueCycle_Succeeds()
    {
        AssertValid(
            """
            type Query {
                field(arg1: A, arg2: B): String
            }

            input A {
                x: A = null
                y: A = { x: null, y: null }
                z: [A] = []
            }

            input B {
                x: B2! = {}
                y: String = "abc"
                z: Custom = {}
            }

            input B2 {
                x: B3 = {}
            }

            input B3 {
                x: B = { x: { x: null } }
            }

            scalar Custom
            """);
    }

    [Fact]
    public void Validate_InputObjectDefaultValueCycle_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg1: A, arg2: B, arg3: C, arg4: D, arg5: E): String
            }

            input A {
                x: A = {}
            }

            input B {
                x: B2 = {}
            }

            input B2 {
                x: B3 = {}
            }

            input B3 {
                x: B = {}
            }

            input C {
                x: [C] = [{}]
            }

            input D {
                x: D = { x: { x: {} } }
            }

            input E {
                x: E = { x: null }
                y: E = { y: null }
            }

            input F {
                x: F2! = {}
            }

            input F2 {
                x: F = { x: {} }
            }
            """,
            """
            {
                "message": "Invalid circular reference. The default value of Input Object field 'A.x' references itself.",
                "code": "HCV0019",
                "severity": "Error",
                "coordinate": "A.x",
                "member": "x",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """,
            """
            {
                "message": "Invalid circular reference. The default value of Input Object field 'B.x' references itself via the default values of: 'B2.x', 'B3.x'.",
                "code": "HCV0019",
                "severity": "Error",
                "coordinate": "B.x",
                "member": "x",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """,
            """
            {
                "message": "Invalid circular reference. The default value of Input Object field 'C.x' references itself.",
                "code": "HCV0019",
                "severity": "Error",
                "coordinate": "C.x",
                "member": "x",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """,
            """
            {
                "message": "Invalid circular reference. The default value of Input Object field 'D.x' references itself.",
                "code": "HCV0019",
                "severity": "Error",
                "coordinate": "D.x",
                "member": "x",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """,
            """
            {
                "message": "Invalid circular reference. The default value of Input Object field 'E.x' references itself via the default values of: 'E.y'.",
                "code": "HCV0019",
                "severity": "Error",
                "coordinate": "E.x",
                "member": "x",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """,
            """
            {
                "message": "Invalid circular reference. The default value of Input Object field 'F2.x' references itself.",
                "code": "HCV0019",
                "severity": "Error",
                "coordinate": "F2.x",
                "member": "x",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """);
    }
}

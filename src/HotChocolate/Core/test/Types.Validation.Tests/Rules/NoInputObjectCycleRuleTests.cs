using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class NoInputObjectCycleRuleTests : RuleTestBase<NoInputObjectCycleRule>
{
    [Fact]
    public void Validate_BreakableCircularReferences_Succeeds()
    {
        AssertValid(
            """
            type Query {
                field(arg: SomeInputObject): String
            }

            input SomeInputObject {
                self: SomeInputObject
                arrayOfSelf: [SomeInputObject]
                nonNullArrayOfSelf: [SomeInputObject]!
                nonNullArrayOfNonNullSelf: [SomeInputObject!]!
                intermediateSelf: AnotherInputObject
            }

            input AnotherInputObject {
                parent: SomeInputObject
            }
            """);
    }

    [Fact]
    public void Validate_NonBreakableCircularReference_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: SomeInputObject): String
            }

            input SomeInputObject {
                nonNullSelf: SomeInputObject!
            }
            """,
            """
            {
                "message": "Invalid circular reference. The Input Object 'SomeInputObject' references itself in the non-null field 'SomeInputObject.nonNullSelf'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "SomeInputObject",
                "member": "SomeInputObject",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_NonBreakableCircularReferenceThroughOtherType_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: SomeInputObject): String
            }

            input SomeInputObject {
                startLoop: AnotherInputObject!
            }

            input AnotherInputObject {
                nextInLoop: YetAnotherInputObject!
            }

            input YetAnotherInputObject {
                closeLoop: SomeInputObject!
            }
            """,
            """
            {
                "message": "Invalid circular reference. The Input Object 'SomeInputObject' references itself via the non-null fields: 'SomeInputObject.startLoop', 'AnotherInputObject.nextInLoop', 'YetAnotherInputObject.closeLoop'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "SomeInputObject",
                "member": "SomeInputObject",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_MultipleNonBreakableCircularReferences_Fails()
    {
        AssertInvalid(
            """
            type Query {
                field(arg: SomeInputObject): String
            }

            input SomeInputObject {
                startLoop: AnotherInputObject!
            }

            input AnotherInputObject {
                closeLoop: SomeInputObject!
                startSecondLoop: YetAnotherInputObject!
            }

            input YetAnotherInputObject {
                closeSecondLoop: AnotherInputObject!
                nonNullSelf: YetAnotherInputObject!
            }
            """,
            """
            {
                "message": "Invalid circular reference. The Input Object 'SomeInputObject' references itself via the non-null fields: 'SomeInputObject.startLoop', 'AnotherInputObject.closeLoop'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "SomeInputObject",
                "member": "SomeInputObject",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """,
            """
            {
                "message": "Invalid circular reference. The Input Object 'AnotherInputObject' references itself via the non-null fields: 'AnotherInputObject.startSecondLoop', 'YetAnotherInputObject.closeSecondLoop'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "AnotherInputObject",
                "member": "AnotherInputObject",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """,
            """
            {
                "message": "Invalid circular reference. The Input Object 'YetAnotherInputObject' references itself in the non-null field 'YetAnotherInputObject.nonNullSelf'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "YetAnotherInputObject",
                "member": "YetAnotherInputObject",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Input-Objects.Type-Validation"
                }
            }
            """);
    }
}

using HotChocolate.Rules;

namespace HotChocolate.Types.Validation.Rules;

public sealed class NoInputObjectUnbreakableCycleRuleTests
    : RuleTestBase<NoInputObjectUnbreakableCycleRule>
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
                "message": "Input Object 'SomeInputObject' cannot be provided a finite value because it references itself through fields: 'SomeInputObject.nonNullSelf'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "SomeInputObject",
                "member": "SomeInputObject",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
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
                "message": "Input Object 'SomeInputObject' cannot be provided a finite value because it references itself through fields: 'SomeInputObject.startLoop', 'AnotherInputObject.nextInLoop', 'YetAnotherInputObject.closeLoop'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "SomeInputObject",
                "member": "SomeInputObject",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
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
                "message": "Input Object 'SomeInputObject' cannot be provided a finite value because it references itself through fields: 'SomeInputObject.startLoop', 'AnotherInputObject.closeLoop'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "SomeInputObject",
                "member": "SomeInputObject",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
                }
            }
            """,
            """
            {
                "message": "Input Object 'AnotherInputObject' cannot be provided a finite value because it references itself through fields: 'AnotherInputObject.startSecondLoop', 'YetAnotherInputObject.closeSecondLoop'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "AnotherInputObject",
                "member": "AnotherInputObject",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
                }
            }
            """,
            """
            {
                "message": "Input Object 'YetAnotherInputObject' cannot be provided a finite value because it references itself through fields: 'YetAnotherInputObject.nonNullSelf'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "YetAnotherInputObject",
                "member": "YetAnotherInputObject",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
                }
            }
            """);
    }
}

using System.Text;
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

    [Fact]
    public void Validate_SelfReferencingOneOf_Fails()
    {
        AssertInvalid(
            """
            input A @oneOf {
                self: A
            }
            """,
            """
            {
                "message": "Input Object 'A' cannot be provided a finite value because it references itself through fields: 'A.self'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "A",
                "member": "A",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
                }
            }
            """);
    }

    [Fact]
    public void Validate_NonOneOfRequiringUnbreakableOneOf_Fails()
    {
        AssertInvalid(
            """
            input T @oneOf {
                self: T
            }

            input A {
                t: T!
            }
            """,
            """
            {
                "message": "Input Object 'T' cannot be provided a finite value because it references itself through fields: 'T.self'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "T",
                "member": "T",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
                }
            }
            """);
    }

    [Fact]
    public void Validate_OneOfAndNonOneOfCycleWithoutEscape_Fails()
    {
        AssertInvalid(
            """
            input A @oneOf {
                b: B
            }

            input B {
                a: A!
            }
            """,
            """
            {
                "message": "Input Object 'A' cannot be provided a finite value because it references itself through fields: 'A.b', 'B.a'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "A",
                "member": "A",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
                }
            }
            """);
    }

    [Fact]
    public void Validate_MultipleOneOfBranchesWithoutEscape_Fails()
    {
        AssertInvalid(
            """
            input A @oneOf {
                b: B
                c: C
            }

            input B {
                a: A!
            }

            input C {
                a: A!
            }
            """,
            """
            {
                "message": "Input Object 'A' cannot be provided a finite value because it references itself through fields: 'A.b', 'B.a'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "A",
                "member": "A",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
                }
            }
            """,
            """
            {
                "message": "Input Object 'A' cannot be provided a finite value because it references itself through fields: 'A.c', 'C.a'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "A",
                "member": "A",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
                }
            }
            """);
    }

    [Fact]
    public void Validate_NonOneOfCycleBesideFiniteRequiredFields_Fails()
    {
        AssertInvalid(
            """
            input A {
                list: [B]!
                finite: Finite!
                b: B!
            }

            input B {
                value: Int!
                a: A!
            }

            input Finite {
                value: Int!
            }
            """,
            """
            {
                "message": "Input Object 'A' cannot be provided a finite value because it references itself through fields: 'A.b', 'B.a'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "A",
                "member": "A",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
                }
            }
            """);
    }

    [Fact]
    public void Validate_ThreeTypeMixedCycleWithoutEscape_Fails()
    {
        AssertInvalid(
            """
            input A @oneOf {
                b: B
            }

            input B {
                c: C!
            }

            input C @oneOf {
                a: A
            }
            """,
            """
            {
                "message": "Input Object 'A' cannot be provided a finite value because it references itself through fields: 'A.b', 'B.c', 'C.a'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "A",
                "member": "A",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
                }
            }
            """);
    }

    [Fact]
    public void Validate_OneOfWithNonNullSelfReference_Fails()
    {
        AssertInvalid(
            """
            input A @oneOf {
                self: A!
            }
            """,
            """
            {
                "message": "Input Object 'A' cannot be provided a finite value because it references itself through fields: 'A.self'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "A",
                "member": "A",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
                }
            }
            """);
    }

    [Fact]
    public void Validate_SharedUnbreakableOneOfSubgraph_Fails()
    {
        // arrange
        // T0 references itself; every T(n) has two fields into T(n-1).
        var sdl = new StringBuilder(
            """
            input T0 @oneOf {
                self: T0
            }
            """);

        for (var i = 1; i <= 16; i++)
        {
            sdl.AppendLine();
            sdl.Append(
                $$"""
                input T{{i}} @oneOf {
                    a: T{{i - 1}}
                    b: T{{i - 1}}
                }
                """);
        }

        // act & assert
        AssertInvalid(
            sdl.ToString(),
            """
            {
                "message": "Input Object 'T0' cannot be provided a finite value because it references itself through fields: 'T0.self'.",
                "code": "HCV0018",
                "severity": "Error",
                "coordinate": "T0",
                "member": "T0",
                "extensions": {
                    "rfc": "https://github.com/graphql/graphql-spec/pull/1211"
                }
            }
            """);
    }

    [Fact]
    public void Validate_OneOfWithScalarField_Succeeds()
    {
        AssertValid(
            """
            input A @oneOf {
                a: Int
            }
            """);
    }

    [Fact]
    public void Validate_OneOfWithRecursiveListField_Succeeds()
    {
        AssertValid(
            """
            input A @oneOf {
                a: [A!]
            }
            """);
    }

    [Fact]
    public void Validate_OneOfReferencingFiniteInputObject_Succeeds()
    {
        AssertValid(
            """
            input A @oneOf {
                b: B
            }

            input B {
                x: Int
            }
            """);
    }

    [Fact]
    public void Validate_OneOfReferencingEarlierDeclaredInputObject_Succeeds()
    {
        AssertValid(
            """
            input B {
                value: Int
            }

            input A @oneOf {
                b: B
            }
            """);
    }

    [Fact]
    public void Validate_OneOfWithMultipleAcyclicInputObjectFields_Succeeds()
    {
        AssertValid(
            """
            input A @oneOf {
                b: B
                c: C
            }

            input B {
                value: Int
            }

            input C {
                value: Int
            }
            """);
    }

    [Fact]
    public void Validate_OneOfCycleWithScalarEscape_Succeeds()
    {
        AssertValid(
            """
            input A @oneOf {
                b: B
                escape: Int
            }

            input B @oneOf {
                a: A
            }
            """);
    }

    [Fact]
    public void Validate_OneOfAndNonOneOfCycleWithNullableEscape_Succeeds()
    {
        AssertValid(
            """
            input A @oneOf {
                b: B
            }

            input B {
                a: A
            }
            """);
    }

    [Fact]
    public void Validate_OneOfAndNonOneOfCycleWithScalarEscape_Succeeds()
    {
        AssertValid(
            """
            input A @oneOf {
                b: B
                escape: Int
            }

            input B {
                a: A!
            }
            """);
    }

    [Fact]
    public void Validate_NonOneOfCycleWithNullableEscape_Succeeds()
    {
        AssertValid(
            """
            input A {
                b: B!
            }

            input B {
                a: A
            }
            """);
    }

    [Fact]
    public void Validate_NonOneOfCycleWithNonNullListEscape_Succeeds()
    {
        AssertValid(
            """
            input A {
                b: [B!]!
            }

            input B {
                a: A!
            }
            """);
    }
}

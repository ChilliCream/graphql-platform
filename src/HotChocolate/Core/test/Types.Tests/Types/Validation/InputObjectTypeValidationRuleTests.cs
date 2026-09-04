using System.Text;
using HotChocolate.Configuration.Validation;

namespace HotChocolate.Types.Validation;

public class InputObjectTypeValidationRuleTests : TypeValidationTestBase
{
    [Fact]
    public void RejectInputTypeWithoutFields()
    {
        ExpectError("""
          type Query { stub: String }

          input Foo {}
        """);
    }

    [Fact]
    public void AcceptInputTypeWithFields()
    {
        ExpectValid("""
          type Query { stub: String }

          input Foo {
              nullable: String
              nonNullable: String!
              defaultNullable: String = "Foo"
              defaultNonNullable: String! = "Foo"
          }
        """);
    }

    [Fact]
    public void AcceptInputTypeWithFieldsAndDirectives()
    {
        ExpectValid("""
          type Query { stub: String }

          input Foo @inputObject {
              nullable: String @inputFieldDefinition
              nonNullable: String! @inputFieldDefinition
              defaultNullable: String = "Foo" @inputFieldDefinition
              defaultNonNullable: String! = "Foo" @inputFieldDefinition
          }

          directive @inputFieldDefinition on INPUT_FIELD_DEFINITION

          directive @inputObject on INPUT_OBJECT
        """);
    }

    [Fact]
    public void RejectFieldsWithInvalidName()
    {
        ExpectError("""
          type Query { stub: String }

          input Foo {
              __badField: String
          }
        """);
    }

    [Fact]
    public void AcceptOneOfWithNullableFields()
    {
        ExpectValid("""
          type Query { stub: String }

          input Foo @oneOf {
              first: String
              second: Int
          }
        """);
    }

    [Fact]
    public void RejectOneOfWithNullableFields()
    {
        ExpectError("""
          type Query { stub: String }

          input Foo @oneOf {
              first: String!
              second: Int!
          }
        """);
    }

    [Fact]
    public void AcceptNonRequiredInputThatIsDeprecated()
    {
        ExpectValid("""
          type Query { stub: String }

          input Foo {
              field: Int @deprecated
          }
        """);
    }

    [Fact]
    public void RejectRequiredFieldThatIsDeprecated()
    {
        ExpectError("""
          type Query { stub: String }

          input Foo {
              field: Int! @deprecated
          }
        """);
    }

    // https://github.com/graphql/graphql-js/pull/1359/files
    [Fact]
    public void AcceptsBreakableCircularReferences()
    {
        ExpectValid("""
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
    public void AcceptsOneOfWithScalarField()
    {
        ExpectValid("""
          type Query { stub: String }

          input A @oneOf {
              a: Int
          }
        """);
    }

    [Fact]
    public void AcceptsOneOfWithRecursiveListField()
    {
        ExpectValid("""
          type Query { stub: String }

          input A @oneOf {
              a: [A!]
          }
        """);
    }

    [Fact]
    public void AcceptsOneOfReferencingFiniteInputObject()
    {
        ExpectValid("""
          type Query { stub: String }

          input A @oneOf {
              b: B
          }

          input B {
              x: Int
          }
        """);
    }

    [Fact]
    public void AcceptsOneOfReferencingEarlierDeclaredInputObject()
    {
        ExpectValid("""
          type Query { stub: String }

          input B {
              value: Int
          }

          input A @oneOf {
              b: B
          }
        """);
    }

    [Fact]
    public void AcceptsOneOfWithMultipleAcyclicInputObjectFields()
    {
        ExpectValid("""
          type Query { stub: String }

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
    public void AcceptsOneOfCycleWithScalarEscape()
    {
        ExpectValid("""
          type Query { stub: String }

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
    public void AcceptsOneOfAndNonOneOfCycleWithNullableEscape()
    {
        ExpectValid("""
          type Query { stub: String }

          input A @oneOf {
              b: B
          }

          input B {
              a: A
          }
        """);
    }

    [Fact]
    public void AcceptsOneOfAndNonOneOfCycleWithScalarEscape()
    {
        ExpectValid("""
          type Query { stub: String }

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
    public void AcceptsNonOneOfCycleWithNullableEscape()
    {
        ExpectValid("""
          type Query { stub: String }

          input A {
              b: B!
          }

          input B {
              a: A
          }
        """);
    }

    [Fact]
    public void AcceptsNonOneOfCycleWithNonNullListEscape()
    {
        ExpectValid("""
          type Query { stub: String }

          input A {
              b: [B!]!
          }

          input B {
              a: A!
          }
        """);
    }

    [Fact]
    public void RejectsNonBreakableDirectCircularReference()
    {
        ExpectError("""
          type Query {
              field(arg: SomeInputObject): String
          }
          input SomeInputObject {
              nonNullSelf: SomeInputObject!
          }
        """);
    }

    [Fact]
    public void RejectsCircularReferenceThroughOtherType()
    {
        ExpectError("""
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
        """);
    }

    [Fact]
    public void RejectsMultipleCircularReferences()
    {
        ExpectError("""
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
        """);
    }

    [Fact]
    public void RejectsSelfReferencingOneOf()
    {
        ExpectError("""
          type Query { stub: String }

          input A @oneOf {
              self: A
          }
        """);
    }

    [Fact]
    public void RejectsNonOneOfRequiringUnbreakableOneOf()
    {
        ExpectError("""
          type Query { stub: String }

          input T @oneOf {
              self: T
          }

          input A {
              t: T!
          }
        """);
    }

    [Fact]
    public void RejectsOneOfAndNonOneOfCycleWithoutEscape()
    {
        ExpectError("""
          type Query { stub: String }

          input A @oneOf {
              b: B
          }

          input B {
              a: A!
          }
        """);
    }

    [Fact]
    public void RejectsMultipleOneOfBranchesWithoutEscape()
    {
        ExpectError("""
          type Query { stub: String }

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
        """);
    }

    [Fact]
    public void RejectsNonOneOfCycleBesideFiniteRequiredFields()
    {
        ExpectError("""
          type Query { stub: String }

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
        """);
    }

    [Fact]
    public void RejectsThreeTypeMixedCycleWithoutEscape()
    {
        ExpectError("""
          type Query { stub: String }

          input A @oneOf {
              b: B
          }

          input B {
              c: C!
          }

          input C @oneOf {
              a: A
          }
        """);
    }

    [Fact]
    public void RejectsOneOfWithNonNullSelfReference()
    {
        ExpectError("""
          type Query { stub: String }

          input A @oneOf {
              self: A!
          }
        """);
    }

    [Fact]
    public void RejectsSharedUnbreakableOneOfSubgraph()
    {
        // arrange
        // T0 references itself; every T(n) has two fields into T(n-1).
        var sdl = new StringBuilder(
            """
            type Query { stub: String }

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
        ExpectError(sdl.ToString());
    }

    [Fact]
    public void RejectInputFieldWithIncompatibleDefaultValue()
    {
        ExpectError("""
          type Query { stub: String }

          input FooInput {
              a: Int = "abc"
          }
        """);
    }
}

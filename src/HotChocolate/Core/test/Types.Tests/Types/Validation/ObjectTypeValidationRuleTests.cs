using HotChocolate.Configuration.Validation;

namespace HotChocolate.Types.Validation;

public class ObjectTypeValidationRuleTests : TypeValidationTestBase
{
    [Fact]
    public void RejectObjectTypeWithoutFields()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo {}
        ");
    }

    [Fact]
    public void AcceptObjectTypeWithFields()
    {
        ExpectValid(@"
          type Query { stub: String }

          type Foo {
              nullable: String
              nonNullable: String!
          }
        ");
    }

    [Fact]
    public void AcceptObjectTypeWithFieldsAndDirectives()
    {
        ExpectValid(@"
          type Query { stub: String }

          type Foo @objectObject {
              nullable: String @objectFieldDefinition
              nonNullable: String! @objectFieldDefinition
          }

          directive @objectFieldDefinition on FIELD_DEFINITION

          directive @objectObject on OBJECT
        ");
    }

    [Fact]
    public void RejectFieldsWithInvalidName()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo {
              __badField: String
          }
        ");
    }

    [Fact]
    public void AcceptInterfaceWithCorrectImplementation()
    {
        ExpectValid(@"
          type Query { stub: String }

          type Foo implements Test {
              first: String
              second(foo: String): String
              third(foo: String!): String
              strengthen: String!
          }
          interface Test {
              first: String
              second(foo: String): String
              third(foo: String!): String
              strengthen: String
          }
        ");
    }

    [Fact]
    public void RejectInterfaceWithMissingImplementation()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo implements Test {
              first: String
          }
          interface Test {
              first: String
              second: String
          }
        ");
    }

    [Fact]
    public void RejectInterfaceWithWrongImplementation()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo implements Test {
              first: Int
          }
          interface Test {
              first: String
          }
        ");
    }

    [Fact]
    public void RejectInterfaceWithMissingImplementationOfArgument()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo implements Test {
              first(first: String): String
          }
          interface Test {
              first(first: String, second: String): String
          }
        ");
    }

    [Fact]
    public void RejectInterfaceWithWrongImplementationOfArgument()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo implements Test {
              first(first: Int): String
          }
          interface Test {
              first(first: String): String
          }
        ");
    }

    [Fact]
    public void RejectInterfaceWithNullableMismatchInImplementationOfArgument()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo implements Test {
              first(first: String): String
          }
          interface Test {
              first(first: String!): String
          }
        ");
    }

    [Fact]
    public void RejectInterfaceWithNullableMismatchInImplementationOfField()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo implements Test {
              first: String
          }
          interface Test {
              first: String!
          }
        ");
    }

    [Fact]
    public void AcceptNonRequiredArgumentThatIsDeprecated()
    {
        ExpectValid(@"
          type Query { stub: String }

          type Foo {
              field(arg: Int @deprecated): String
          }
        ");
    }

    [Fact]
    public void RejectNonRequiredArgumentThatIsDeprecated()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo {
              field(arg: Int! @deprecated): String
          }
        ");
    }

    [Fact]
    public void AcceptArgumentWithCompatibleDefaultValue()
    {
        ExpectValid(@"
          type Query { stub: String }

          type Foo {
              field(arg: Int! = 123): String
          }
        ");
    }

    [Fact]
    public void RejectArgumentWithIncompatibleScalarDefaultValue()
    {
        ExpectError("""
          type Query { stub: String }

          type Foo {
              field(arg: Int = "abc"): String
          }
        """);
    }

    [Fact]
    public void RejectArgumentWithNullDefaultValueForNonNull()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo {
              field(arg: Int! = null): String
          }
        ");
    }

    [Fact]
    public void RejectArgumentWithUnknownInputObjectFieldDefault()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo {
              field(arg: FooInput = { missing: 1 }): String
          }

          input FooInput { a: Int }
        ");
    }

    [Fact]
    public void RejectArgumentWithMissingRequiredInputFieldDefault()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo {
              field(arg: FooInput = {}): String
          }

          input FooInput {
              a: Int
              b: Int!
          }
        ");
    }

    [Fact]
    public void RejectArgumentWithNestedIncompatibleDefaultValue()
    {
        ExpectError("""
          type Query { stub: String }

          type Foo {
              field(arg: BarInput = { inner: { a: "abc" } }): String
          }

          input BarInput { inner: FooInput }

          input FooInput { a: Int }
        """);
    }

    [Fact]
    public void RejectArgumentWithUndefinedEnumDefaultValue()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo {
              field(arg: FooEnum = MISSING): String
          }

          enum FooEnum { VALUE }
        ");
    }

    [Fact]
    public void RejectArgumentWithOneOfDefaultSettingMultipleFields()
    {
        ExpectError(@"
          type Query { stub: String }

          type Foo {
              field(arg: FooOneOf = { a: 1, b: 2 }): String
          }

          input FooOneOf @oneOf {
              a: Int
              b: Int
          }
        ");
    }
}

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
    public void RejectInterfaceWithNullableMissMatchInImplementationOfArgument()
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
    public void RejectInterfaceWithNullableMissMatchInImplementationOfField()
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
}

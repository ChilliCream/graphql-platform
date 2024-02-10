using HotChocolate.Configuration.Validation;

namespace HotChocolate.Types.Validation;

public class InterfaceTypeValidationRuleTests : TypeValidationTestBase
{
    [Fact]
    public void RejectInterfaceTypeWithoutFields()
    {
        ExpectError(@"
          type Query { stub: String }

          type Bar implements Foo { str: String! }

          interface Foo {}
        ");
    }

    [Fact]
    public void AcceptInterfaceTypeWithFields()
    {
        ExpectValid(@"
          type Query { stub: String }

          type Bar implements Foo {
              nullable: String
              nonNullable: String!
          }

          interface Foo {
              nullable: String
              nonNullable: String!
          }
        ");
    }

    [Fact]
    public void AcceptInterfaceTypeWithFieldsAndDirectives()
    {
        ExpectValid(@"
          type Query { stub: String }

          type Bar implements Foo {
              nullable: String
              nonNullable: String!
          }

          interface Foo @interfaceInterface {
              nullable: String @interfaceFieldDefinition
              nonNullable: String! @interfaceFieldDefinition
          }

          directive @interfaceFieldDefinition on FIELD_DEFINITION

          directive @interfaceInterface on INTERFACE
        ");
    }

    [Fact]
    public void RejectFieldsWithInvalidName()
    {
        ExpectError(@"
          type Query { stub: String }

          type Bar implements Foo {
              nonNullable: String!
          }

          interface Foo {
              __badField: String
          }
        ");
    }

    [Fact]
    public void AcceptInterfaceWithCorrectImplementation()
    {
        ExpectValid(@"
          type Query { stub: String }

          type Bar implements Foo {
              first: String
          }

          interface Foo implements Test {
              first: String
          }
          interface Test {
              first: String
          }
        ");
    }

   [Fact]
    public void AcceptNonRequiredArgumentThatIsDeprecated()
    {
        ExpectValid(@"
          type Query { stub: String }

          interface Foo {
              field(arg: Int @deprecated): String
          }
        ");
    }

    [Fact]
    public void RejectNonRequiredArgumentThatIsDeprecated()
    {
        ExpectError(@"
          type Query { stub: String }

          type Bar implements Foo {
              field(arg: Int! @deprecated): String
          }

          interface Foo {
              field(arg: Int! @deprecated): String
          }
        ");
    }
}

using HotChocolate.Configuration.Validation;
using Xunit;

namespace HotChocolate.Types.Validation;

public class InputObjectTypeValidationRuleTests : TypeValidationTestBase
{
    [Fact]
    public void RejectInputTypeWithoutFields()
    {
        ExpectError(@"
          type Query { stub: String }

          input Foo {}
        ");
    }

    [Fact]
    public void AcceptInputTypeWithFields()
    {
        ExpectValid(@"
          type Query { stub: String }

          input Foo {
              nullable: String
              nonNullable: String!
              defaultNullable: String = ""Foo""
              defaultNonNullable: String! = ""Foo""
          }
        ");
    }

    [Fact]
    public void AcceptInputTypeWithFieldsAndDirectives()
    {
        ExpectValid(@"
          type Query { stub: String }

          input Foo @inputObject {
              nullable: String @inputFieldDefinition
              nonNullable: String! @inputFieldDefinition
              defaultNullable: String = ""Foo"" @inputFieldDefinition
              defaultNonNullable: String! = ""Foo"" @inputFieldDefinition
          }

          directive @inputFieldDefinition on INPUT_FIELD_DEFINITION

          directive @inputObject on INPUT_OBJECT
        ");
    }

    [Fact]
    public void RejectFieldsWithInvalidName()
    {
        ExpectError(@"
          type Query { stub: String }

          input Foo {
              __badField: String
          }
        ");
    }

    [Fact]
    public void AcceptOneOfWithNullableFields()
    {
        ExpectValid(@"
          type Query { stub: String }

          input Foo @oneOf {
              first: String
              second: Int
          }
        ");
    }

    [Fact]
    public void RejectOneOfWithNullableFields()
    {
        ExpectError(@"
          type Query { stub: String }

          input Foo @oneOf {
              first: String!
              second: Int!
          }
        ");
    }

    [Fact]
    public void AcceptNonRequiredInputThatIsDeprecated()
    {
        ExpectValid(@"
          type Query { stub: String }

          input Foo {
              field: Int @deprecated
          }
        ");
    }

    [Fact]
    public void RejectRequiredFieldThatIsDeprecated()
    {
        ExpectError(@"
          type Query { stub: String }

          input Foo {
              field: Int! @deprecated
          }
        ");
    }
}

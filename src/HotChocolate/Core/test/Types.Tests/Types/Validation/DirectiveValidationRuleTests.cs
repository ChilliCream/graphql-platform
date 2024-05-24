using HotChocolate.Configuration.Validation;

namespace HotChocolate.Types.Validation;

public class DirectiveValidationRuleTests : TypeValidationTestBase
{
    [Fact]
    public void RejectRequiredArgumentThatIsDeprecated()
    {
        ExpectError(@"
          type Query { stub: String }

          directive @badDirective(
             badArg: String! @deprecated
           ) on FIELD
        ");
    }

    [Fact]
    public void AcceptNonRequiredArgumentThatIsDeprecated()
    {
        ExpectValid(@"
          type Query { stub: String }

          directive @badDirective(
             optionalArg: String @deprecated
           ) on FIELD
        ");
    }

    [Fact]
    public void RejectArgumentsWithInvalidName()
    {
        ExpectError(@"
          type Query { stub: String }

          directive @badDirective(
             __badArg: String
           ) on FIELD
        ");
    }

    [Fact]
    public void RejectDirectiveWithInvalidName()
    {
        ExpectError(@"
          type Query { stub: String }

          directive @__badDirective() on FIELD
        ");
    }
}

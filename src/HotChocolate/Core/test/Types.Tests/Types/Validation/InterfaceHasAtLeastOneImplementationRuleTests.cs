using HotChocolate.Configuration.Validation;

namespace HotChocolate.Types.Validation;

public class InterfaceHasAtLeastOneImplementationRuleTests : TypeValidationTestBase
{
    [Fact]
    public void RejectInterfaceWithNoImplementor()
    {
        ExpectError(@"
          type Query { stub: Foo }

          interface Foo {string: String}
        ");
    }

    [Fact]
    public void AcceptInterfaceWithOneImplementor()
    {
        ExpectValid(@"
          type Query { stub: Foo }

          type Bar implements Foo { string: String }

          interface Foo { string: String }
        ");
    }
}

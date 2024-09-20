using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class LoneAnonymousOperationRuleTests
    : DocumentValidatorVisitorTestBase
{
    public LoneAnonymousOperationRuleTests()
        : base(builder => builder.AddOperationRules())
    {
    }

    [Fact]
    public void QueryContainsOneAnonymousOperation()
    {
        ExpectValid(@"
                {
                    dog {
                        name
                    }
                }
            ");
    }

    [Fact]
    public void QueryWithOneAnonymousAndOneNamedOperation()
    {
        ExpectErrors(@"
                {
                    dog {
                        name
                    }
                }

                query getName {
                    dog {
                        owner {
                            name
                        }
                    }
                }
            ",
            t => Assert.Equal(
                "GraphQL allows a short‐hand form for defining query " +
                "operations when only that one operation exists in the " +
                "document.", t.Message));
    }

    [Fact]
    public void QueryWithTwoAnonymousOperations()
    {
        ExpectErrors(@"
                {
                    dog {
                        name
                    }
                }

                {
                    dog {
                        name
                    }
                }
            ",
            t => Assert.Equal(
                "GraphQL allows a short‐hand form for defining query " +
                "operations when only that one operation exists in the " +
                "document.", t.Message));
    }

    [Fact]
    public void MultipleNamedOperations()
    {
        ExpectValid(@"
                query Foo {
                    dog {
                        name
                    }
                }
                query Bar {
                    dog {
                        name
                    }
                }
            ");
    }

    [Fact]
    public void AnonymousOperationWithFragment()
    {
        ExpectValid(@"
                {
                    ...Foo
                }
                fragment Foo on Query {
                    dog {
                        name
                    }
                }
            ");
    }

    [Fact]
    public void AnonymousOperationWithAMutation()
    {
        ExpectErrors(@"
                {
                    dog {
                        name
                    }
                }
                mutation Foo {
                    fieldB
                }
            ");
    }

    [Fact]
    public void AnonymousOperationWithASubscription()
    {
        ExpectErrors(@"
                {
                    dog {
                        name
                    }
                }
                subscription Foo {
                    newMessage
                }
            ");
    }
}

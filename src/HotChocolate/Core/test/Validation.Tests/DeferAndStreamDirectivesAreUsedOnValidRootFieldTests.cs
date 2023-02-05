using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class DeferAndStreamDirectivesAreUsedOnValidRootFieldTests
    : DocumentValidatorVisitorTestBase
{
    public DeferAndStreamDirectivesAreUsedOnValidRootFieldTests()
        : base(builder => builder.AddOperationRules())
    {
    }

    [Fact]
    public void Defer_On_Subscriptions_Root()
    {
        ExpectErrors(
            @"subscription {
                ... @defer {
                    disallowedSecondRootField
                }
            }",
            t => Assert.Equal(
                "The defer and stream directives are not allowed to " +
                "be used on root fields of the mutation or subscription type.",
                t.Message));
    }

    [Fact]
    public void Defer_On_Subscriptions_Root_In_Nested_Fragment()
    {
        ExpectErrors(
            @"subscription {
                ... a
            }

            fragment a on Subscription {
                ... b
            }

            fragment b on Subscription {
                ... @defer {
                    disallowedSecondRootField
                }
            }",
            t => Assert.Equal(
                "The defer and stream directives are not allowed to " +
                "be used on root fields of the mutation or subscription type.",
                t.Message));
    }

    [Fact]
    public void Stream_On_Subscriptions_Root()
    {
        ExpectErrors(
            @"subscription {
                listEvent @stream
            }",
            t => Assert.Equal(
                "The defer and stream directives are not allowed to " +
                "be used on root fields of the mutation or subscription type.",
                t.Message));
    }

    [Fact]
    public void Defer_On_Subscriptions_Sub_Selection()
    {
        ExpectValid(
            @"subscription {
                newMessage {
                    ... @defer {
                        body
                    }
                }
            }");
    }
}

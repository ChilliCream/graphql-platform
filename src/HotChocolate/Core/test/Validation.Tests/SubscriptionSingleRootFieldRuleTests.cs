using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class SubscriptionSingleRootFieldRuleTests
    : DocumentValidatorVisitorTestBase
{
    public SubscriptionSingleRootFieldRuleTests()
        : base(builder => builder.AddOperationRules())
    {
    }

    [Fact]
    public void SubscriptionWithOneRootField()
    {
        ExpectValid(
            """
            subscription sub {
              newMessage {
                body
                sender
              }
            }
            """
        );
    }

    [Fact]
    public void SubscriptionWithOneRootFieldAnonymous()
    {
        ExpectValid(
            """
            subscription {
              newMessage {
                body
                sender
              }
            }
            """
        );
    }

    [Fact]
    public void SubscriptionWithDirectiveThatContainsOneRootField()
    {
        // arrange
        ExpectValid(
            """
            subscription sub {
              ...newMessageFields
            }

            fragment newMessageFields on Subscription {
              newMessage {
                body
                sender
              }
            }
            """
        );
    }

    [Fact]
    public void DisallowedSecondRootField()
    {
        ExpectErrors(
            """
            subscription sub {
              newMessage {
                body
                sender
              }
              disallowedSecondRootField
            }
            """,
            t => Assert.Equal(
                $"Subscription operations must "
                + "have exactly one root field.", t.Message));
    }

    [Fact]
    public void DisallowedSecondRootFieldAnonymous()
    {
        ExpectErrors(
            """
            subscription sub {
              newMessage {
                body
                sender
              }
              disallowedSecondRootField
            }
            """,
            t => Assert.Equal(
                $"Subscription operations must "
                + "have exactly one root field.", t.Message));
    }

    [Fact]
    public void FailsWithManyMoreThanOneRootField()
    {
        ExpectErrors(
            """
            subscription sub {
              newMessage {
                body
                sender
              }
              disallowedSecondRootField
              disallowedThirdRootField
            }
            """,
            t => Assert.Equal(
                $"Subscription operations must "
                + "have exactly one root field.", t.Message));
    }

    [Fact]
    public void DisallowedSecondRootFieldWithinDirective()
    {
        ExpectErrors(
            """
            subscription sub {
              ...multipleSubscriptions
            }

            fragment multipleSubscriptions on Subscription {
              newMessage {
                body
                sender
              }
              disallowedSecondRootField
            }
            """,
            t => Assert.Equal(
                $"Subscription operations must "
                + "have exactly one root field.", t.Message));
    }

    [Fact]
    public void DisallowedSkipDirectiveOnRootField()
    {
        ExpectErrors(@"
                subscription requiredRuntimeValidation($bool: Boolean!) {
                    newMessage @skip(if: $bool) {
                        body
                        sender
                    }
                }
            ",
            t => Assert.Equal(
                "The skip and include directives are not allowed to be used on root fields of "
                + "the subscription type.",
                t.Message));
    }

    [Fact]
    public void DisallowedIncludeDirectiveOnRootField()
    {
        ExpectErrors(@"
                subscription requiredRuntimeValidation($bool: Boolean!) {
                    newMessage @include(if: $bool) {
                        body
                        sender
                    }
                }
            ",
            t => Assert.Equal(
                "The skip and include directives are not allowed to be used on root fields of "
                + "the subscription type.",
                t.Message));
    }

    [Fact]
    public void DisallowedSkipDirectiveOnRootFieldWithinFragment()
    {
        // arrange
        ExpectErrors(@"
                subscription sub {
                    ...newMessageFields
                }

                fragment newMessageFields on Subscription {
                    newMessage @skip(if: true) {
                        body
                        sender
                    }
                }
            ",
            t => Assert.Equal(
                "The skip and include directives are not allowed to be used on root fields of "
                + "the subscription type.",
                t.Message));
    }

    [Fact]
    public void DisallowedIncludeDirectiveOnRootFieldWithinFragment()
    {
        // arrange
        ExpectErrors(@"
                subscription sub {
                    ...newMessageFields
                }

                fragment newMessageFields on Subscription {
                    newMessage @include(if: true) {
                        body
                        sender
                    }
                }
            ",
            t => Assert.Equal(
                "The skip and include directives are not allowed to be used on root fields of "
                + "the subscription type.",
                t.Message));
    }

    [Fact]
    public void DisallowedSkipDirectiveOnRootFieldWithinInlineFragment()
    {
        // arrange
        ExpectErrors(@"
                subscription sub {
                    ...on Subscription {
                        newMessage @skip(if: true) {
                            body
                            sender
                        }
                    }
                }
            ",
            t => Assert.Equal(
                "The skip and include directives are not allowed to be used on root fields of "
                + "the subscription type.",
                t.Message));
    }

    [Fact]
    public void DisallowedIncludeDirectiveOnRootFieldWithinInlineFragment()
    {
        // arrange
        ExpectErrors(@"
                subscription sub {
                    ...on Subscription {
                        newMessage @include(if: true) {
                            body
                            sender
                        }
                    }
                }
            ",
            t => Assert.Equal(
                "The skip and include directives are not allowed to be used on root fields of "
                + "the subscription type.",
                t.Message));
    }

    [Fact]
    public void DisallowedIntrospectionField()
    {
        ExpectErrors(
            """
            subscription sub {
              newMessage {
                body
                sender
              }
              __typename
            }
            """,
            t => Assert.Equal(
                $"Subscription operations must "
                + "have exactly one root field.", t.Message));
    }

    [Fact]
    public void DisallowedOnlyIntrospectionField()
    {
        ExpectErrors(
            """
            subscription sub {
              __typename
            }
            """,
            t => Assert.Equal(
                "Subscription must not select an introspection top level field.", t.Message));
    }
}

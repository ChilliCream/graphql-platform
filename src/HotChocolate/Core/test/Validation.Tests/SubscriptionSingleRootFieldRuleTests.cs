using Xunit;

namespace HotChocolate.Validation
{
    public class SubscriptionSingleRootFieldRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public SubscriptionSingleRootFieldRuleTests()
            : base(services => services.AddOperationRules())
        {
        }

        [Fact]
        public void SubscriptionWithOneRootField()
        {
            ExpectValid(@"
                subscription sub {
                    newMessage {
                        body
                        sender
                    }
                }
            ");
        }

        [Fact]
        public void SubscriptionWithDirectiveThatContainsOneRootField()
        {
            // arrange
            ExpectValid(@"
                subscription sub {
                    ...newMessageFields
                }

                fragment newMessageFields on Subscription {
                    newMessage {
                        body
                        sender
                    }
                }
            ");
        }

        [Fact]
        public void DisallowedSecondRootField()
        {
            ExpectErrors(@"
                subscription sub {
                    newMessage {
                        body
                        sender
                    }
                    disallowedSecondRootField
                }
            ",
            t => Assert.Equal(
                $"Subscription operations must " +
                "have exactly one root field.", t.Message));
        }

        [Fact]
        public void DisallowedSecondRootFieldWithinDirective()
        {
            ExpectErrors(@"
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
            ",
            t => Assert.Equal(
                $"Subscription operations must " +
                "have exactly one root field.", t.Message));
        }

        [Fact]
        public void DisallowedIntrospectionField()
        {
            ExpectErrors(@"
                subscription sub {
                    newMessage {
                        body
                        sender
                    }
                    __typename
                }
            ",
            t => Assert.Equal(
                $"Subscription operations must " +
                "have exactly one root field.", t.Message));
        }
    }
}

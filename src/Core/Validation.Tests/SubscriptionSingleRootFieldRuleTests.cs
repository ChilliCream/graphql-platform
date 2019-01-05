using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class SubscriptionSingleRootFieldRuleTests
        : ValidationTestBase
    {
        public SubscriptionSingleRootFieldRuleTests()
            : base(new SubscriptionSingleRootFieldRule())
        {
        }

        [Fact]
        public void SubscriptionWithOneRootField()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                subscription sub {
                    newMessage {
                        body
                        sender
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void SubscriptionWithDirectiveThatContainsOneRootField()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void DisallowedSecondRootField()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                subscription sub {
                    newMessage {
                        body
                        sender
                    }
                    disallowedSecondRootField
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"Subscription operation `sub` must " +
                    "have exactly one root field.", t.Message));
        }

        [Fact]
        public void DisallowedSecondRootFieldWithinDirective()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"Subscription operation `sub` must " +
                    "have exactly one root field.", t.Message));
        }

        [Fact]
        public void DisallowedIntrospectionField()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                subscription sub {
                    newMessage {
                        body
                        sender
                    }
                    __typename
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"Subscription operation `sub` must " +
                    "have exactly one root field.", t.Message));
        }
    }
}

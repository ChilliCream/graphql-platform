using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class LoneAnonymousOperationRuleTests
    {
        [Fact]
        public void QueryContainsOneAnonymousOperation()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        name
                    }
                }
            ");

            // act
            var validator = new LoneAnonymousOperationRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void QueryWithOneAnonymousAndOneNamedOperation()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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
            ");

            // act
            var validator = new LoneAnonymousOperationRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t =>
                {
                    Assert.Equal(
                        "GraphQL allows a short‐hand form for defining query " +
                        "operations when only that one operation exists in the " +
                        "document.", t.Message);
                });
        }

        [Fact]
        public void QueryWithTwoAnonymousOperations()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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
            ");

            // act
            var validator = new LoneAnonymousOperationRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t =>
                {
                    Assert.Equal(
                        "GraphQL allows a short‐hand form for defining query " +
                        "operations when only that one operation exists in the " +
                        "document.", t.Message);
                });
        }
    }
}

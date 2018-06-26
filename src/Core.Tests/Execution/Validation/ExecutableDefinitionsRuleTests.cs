using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Execution.Validation
{
    public class ExecutableDefinitionsRuleTests
    {
        [Fact]
        public void QueryWithTypeSystemDefinitions()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query getDogName {
                    dog {
                        name
                        color
                    }
                }

                extend type Dog {
                    color: String
                }
            ");

            // act
            ExecutableDefinitionsRule validator =
                new ExecutableDefinitionsRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "A document containing TypeSystemDefinition " +
                    "is invalid for execution.", t.Message));
        }

        [Fact]
        public void QueryWithoutTypeSystemDefinitions()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query getDogName {
                    dog {
                        name
                        color
                    }
                }
            ");

            // act
            ExecutableDefinitionsRule validator =
                new ExecutableDefinitionsRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
            Assert.Empty(result.Errors);
        }
    }
}

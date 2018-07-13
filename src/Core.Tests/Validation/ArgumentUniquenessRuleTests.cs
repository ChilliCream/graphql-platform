using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class ArgumentUniquenessRuleTests
    {

    }

    public class RequiredArgumentRuleTests
    {
        [Fact]
        public void QueryWithTypeSystemDefinitions()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment goodBooleanArg on Arguments {
                    booleanArgField(booleanArg: true)
                }

                fragment goodNonNullArg on Arguments {
                    nonNullBooleanArgField(nonNullBooleanArg: true)
                }
            ");

            // act
            var validator = new RequiredArgumentRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void QueryWithTypeSystemDefinitions()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment goodBooleanArgDefault on Arguments {
                    booleanArgField
                }
            ");

            // act
            var validator = new RequiredArgumentRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void QueryWithTypeSystemDefinitions()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment missingRequiredArg on Arguments {
                    nonNullBooleanArgField
                }
            ");

            // act
            var validator = new RequiredArgumentRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void QueryWithTypeSystemDefinitions()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment missingRequiredArg on Arguments {
                    nonNullBooleanArgField(nonNullBooleanArg: null)
                }
            ");

            // act
            var validator = new RequiredArgumentRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
        }
    }
}

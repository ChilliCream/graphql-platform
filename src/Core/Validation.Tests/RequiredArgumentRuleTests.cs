using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class RequiredArgumentRuleTests
        : ValidationTestBase
    {
        public RequiredArgumentRuleTests()
            : base(new RequiredArgumentRule())
        {
        }

        [Fact]
        public void BooleanArgFieldAndNonNullBooleanArgField()
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
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void GoodBooleanArgDefault()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment goodBooleanArgDefault on Arguments {
                    booleanArgField
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void MissingRequiredArg()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment missingRequiredArg on Arguments {
                    nonNullBooleanArgField
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"The argument `nonNullBooleanArg` is required " +
                    "and does not allow null values.", t.Message));
        }

        [Fact]
        public void MissingRequiredArgNonNullBooleanArg()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment missingRequiredArg on Arguments {
                    nonNullBooleanArgField(nonNullBooleanArg: null)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"The argument `nonNullBooleanArg` is required " +
                    "and does not allow null values.", t.Message));
        }

        [Fact]
        public void MissingRequiredDirectiveArg()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment missingRequiredArg on Arguments {
                    nonNullBooleanArgField(nonNullBooleanArg: true) @skip()
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"The argument `if` is required " +
                    "and does not allow null values.", t.Message));
        }
    }
}

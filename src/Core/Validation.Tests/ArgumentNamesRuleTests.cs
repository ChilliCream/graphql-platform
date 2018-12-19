using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class ArgumentNamesRuleTests
        : ValidationTestBase
    {
        public ArgumentNamesRuleTests()
            : base(new ArgumentNamesRule())
        {
        }

        [Fact]
        public void ArgOnRequiredArg()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment argOnRequiredArg on Dog {
                    doesKnowCommand(dogCommand: SIT)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ArgOnOptional()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment argOnOptional on Dog {
                    isHousetrained(atOtherHomes: true) @include(if: true)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void InvalidFieldArgName()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment invalidArgName on Dog {
                    doesKnowCommand(command: CLEAN_UP_HOUSE)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"The argument `command` does not exist.", t.Message));
        }

        [Fact]
        public void InvalidDirectiveArgName()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment invalidArgName on Dog {
                    isHousetrained(atOtherHomes: true) @include(unless: false)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"The argument `unless` does not exist.", t.Message));
        }

        [Fact]
        public void ArgumentOrderDoesNotMatter()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment multipleArgs on Arguments {
                    multipleReqs(x: 1, y: 2)
                }

                fragment multipleArgsReverseOrder on Arguments {
                    multipleReqs(y: 1, x: 2)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }
    }
}

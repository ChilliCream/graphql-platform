using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class DirectivesAreUniquePerLocationRuleTests
        : ValidationTestBase
    {
        public DirectivesAreUniquePerLocationRuleTests()
            : base(new DirectivesAreUniquePerLocationRule())
        {
        }

        [Fact]
        public void DuplicateSkipDirectives()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query ($foo: Boolean = true, $bar: Boolean = false) {
                    field @skip(if: $foo) @skip(if: $bar)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "Only one of each directive is allowed per location.",
                    t.Message));
        }

        [Fact]
        public void SkipOnTwoDifferentFields()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query ($foo: Boolean = true, $bar: Boolean = false) {
                    field @skip(if: $foo) {
                        subfieldA
                    }
                    field @skip(if: $bar) {
                        subfieldB
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }
    }
}

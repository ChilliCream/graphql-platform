using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class DirectivesAreInValidLocationsRuleTests
        : ValidationTestBase
    {
        public DirectivesAreInValidLocationsRuleTests()
            : base(new DirectivesAreInValidLocationsRule())
        {
        }

        [Fact]
        public void SkipDirectiveIsInTheWrongPlace()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query @skip(if: $foo) {
                    field
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified directive is not valid the " +
                    "current location.", t.Message));
        }

        [Fact]
        public void SkipDirectiveIsInTheRightPlace()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query a {
                    field @skip(if: $foo)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

    }
}

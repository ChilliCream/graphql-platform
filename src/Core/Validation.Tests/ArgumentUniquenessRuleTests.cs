using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class ArgumentUniquenessRuleTests
        : ValidationTestBase
    {
        public ArgumentUniquenessRuleTests()
            : base(new ArgumentUniquenessRule())
        {
        }

        [Fact]
        public void NoDuplicateArgument()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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
        public void DuplicateArgument()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment goodNonNullArg on Arguments {
                    nonNullBooleanArgField(nonNullBooleanArg: true, nonNullBooleanArg: true)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"Arguments are not unique.", t.Message));
        }
    }
}

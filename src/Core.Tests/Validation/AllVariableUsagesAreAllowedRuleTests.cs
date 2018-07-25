using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class AllVariableUsagesAreAllowedRuleTests
        : ValidationTestBase
    {
        public AllVariableUsagesAreAllowedRuleTests()
            : base(new AllVariableUsagesAreAllowedRule())
        {
        }

        [Fact]
        public void IntCannotGoIntoBoolean()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query intCannotGoIntoBoolean($intArg: Int) {
                    arguments {
                        booleanArgField(booleanArg: $intArg)
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The variable `intArg` type is not " +
                    "compatible with the type of the " +
                    "argument `booleanArg`.",
                    t.Message));
        }
    }
}

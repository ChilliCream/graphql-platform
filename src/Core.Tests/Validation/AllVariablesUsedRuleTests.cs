using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class AllVariablesUsedRuleTests
    {
        [Fact]
        public void VariableUnused()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query variableUnused($atOtherHomes: Boolean) {
                    dog {
                        isHousetrained
                    }
                }
            ");

            // act
            var validator = new AllVariablesUsedRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The following variables were not used: " +
                    "atOtherHomes.", t.Message));
        }

        [Fact]
        public void VariableUsedInFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query variableUsedInFragment($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    isHousetrained(atOtherHomes: $atOtherHomes)
                }
            ");

            // act
            var validator = new AllVariablesUsedRule();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }
    }
}

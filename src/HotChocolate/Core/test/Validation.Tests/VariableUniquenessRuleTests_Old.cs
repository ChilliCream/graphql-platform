using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class VariableUniquenessRuleTests_Old
        : ValidationTestBase
    {
        public VariableUniquenessRuleTests_Old()
            : base(new VariableUniquenessRule())
        {
        }

        [Fact]
        public void OperationWithTwoVariablesThatHaveTheSameName()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query houseTrainedQuery($atOtherHomes: Boolean, $atOtherHomes: Boolean) {
                    dog {
                        isHousetrained(atOtherHomes: $atOtherHomes)
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "A document containing operations that " +
                    "define more than one variable with the same " +
                    "name is invalid for execution.", t.Message));
        }

        [Fact]
        public void NoOperationHasVariablesThatShareTheSameName()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
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

        [Fact]
        public void TwoOperationsThatShareVariableName()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query A($atOtherHomes: Boolean) {
                  ...HouseTrainedFragment
                }

                query B($atOtherHomes: Boolean) {
                  ...HouseTrainedFragment
                }

                fragment HouseTrainedFragment on Query {
                  dog {
                    isHousetrained(atOtherHomes: $atOtherHomes)
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

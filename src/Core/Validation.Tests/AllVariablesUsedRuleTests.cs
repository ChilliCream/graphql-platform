using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class AllVariablesUsedRuleTests
        : ValidationTestBase
    {
        public AllVariablesUsedRuleTests()
            : base(new AllVariablesUsedRule())
        {
        }

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
            QueryValidationResult result = Rule.Validate(schema, query);

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
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void VariableUsedInSecondLevelFragment()
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
                    ...isHousetrainedFragmentLevel2
                }

                fragment isHousetrainedFragmentLevel2 on Dog {
                    isHousetrained(atOtherHomes: $atOtherHomes)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void VariableUsedInDirective()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode querya = Parser.Default.Parse(@"
                query variableUsedInFragment($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    isHousetrained @skip(if: $atOtherHomes)
                }
            ");

            DocumentNode queryb = Parser.Default.Parse(@"
                query variableUsedInFragment($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment @skip(if: $atOtherHomes)
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    isHousetrained
                }
            ");

            // act
            QueryValidationResult resulta = Rule.Validate(schema, querya);
            QueryValidationResult resultb = Rule.Validate(schema, queryb);

            // assert
            Assert.False(resulta.HasErrors);
            Assert.False(resultb.HasErrors);
        }


        [Fact]
        public void VariableNotUsedWithinFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query variableNotUsedWithinFragment($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedWithoutVariableFragment
                    }
                }

                fragment isHousetrainedWithoutVariableFragment on Dog {
                    isHousetrained
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The following variables were not used: " +
                    "atOtherHomes.", t.Message));
        }

        [Fact]
        public void QueryWithExtraVar()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query queryWithUsedVar($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                query queryWithExtraVar($atOtherHomes: Boolean, $extra: Int) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    isHousetrained(atOtherHomes: $atOtherHomes)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The following variables were not used: " +
                    "extra.", t.Message));
        }
    }
}

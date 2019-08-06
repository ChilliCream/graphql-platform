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
            DocumentNode query = Utf8GraphQLParser.Parse(@"
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
            DocumentNode query = Utf8GraphQLParser.Parse(@"
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
            DocumentNode query = Utf8GraphQLParser.Parse(@"
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
            DocumentNode querya = Utf8GraphQLParser.Parse(@"
                query variableUsedInFragment($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    isHousetrained @skip(if: $atOtherHomes)
                }
            ");

            DocumentNode queryb = Utf8GraphQLParser.Parse(@"
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
            DocumentNode query = Utf8GraphQLParser.Parse(@"
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
            DocumentNode query = Utf8GraphQLParser.Parse(@"
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

        [Fact]
        public void VariableUsedAndDeclared()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query variableIsDefined($atOtherHomes: Boolean)
                {
                    dog {
                        isHousetrained(atOtherHomes: $atOtherHomes)
                    }
                }");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void VariableUsedInComplexInput()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query queryWithComplexInput($name: String)
                {
                    findDog(complex: { name: $name }) {
                        name
                    }
                }");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void VariableUsedInListInput()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query queryWithListInput($value: Bool)
                {
                    booleanList(booleanListArg: [ $value ])
                }");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }


        [Fact]
        public void VariableUsedAndNotDeclared()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query variableIsDefined
                {
                    dog {
                        isHousetrained(atOtherHomes: $atOtherHomes)
                    }
                }");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The following variables were not declared: " +
                    "atOtherHomes.", t.Message));
        }

        [Fact]
        public void VariableUsedAndNotDeclared2()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query variableIsNotDefinedUsedInNestedFragment {
                    dog {
                        ...outerHousetrainedFragment
                    }
                }

                fragment outerHousetrainedFragment on Dog {
                    ...isHousetrainedFragment
                }

                fragment isHousetrainedFragment on Dog {
                    isHousetrained(atOtherHomes: $atOtherHomes)
                }");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The following variables were not declared: " +
                    "atOtherHomes.", t.Message));
        }

        [Fact]
        public void VarsMustBeDefinedInAllOperationsInWhichAFragmentIsUsed()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query housetrainedQueryOne($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                query housetrainedQueryTwo($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                query housetrainedQueryThree {
                    dog {
                        isHousetrained(atOtherHomes: true)
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    isHousetrained(atOtherHomes: $atOtherHomes)
                }");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void VarsMustBeDefinedInAllOperationsInWhichAFragmentIsUsedErr()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query variableIsNotDefinedUsedInNestedFragment {
                    dog {
                        ...outerHousetrainedFragment
                    }
                }

                fragment outerHousetrainedFragment on Dog {
                    ...isHousetrainedFragment
                }

                fragment isHousetrainedFragment on Dog {
                    isHousetrained(atOtherHomes: $atOtherHomes)
                }");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The following variables were not declared: " +
                    "atOtherHomes.", t.Message));
        }
    }
}

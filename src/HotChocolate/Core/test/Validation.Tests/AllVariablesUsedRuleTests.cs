using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class AllVariablesUsedRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public AllVariablesUsedRuleTests()
            : base(services => services.AddAllVariablesUsedRule())
        {
        }

        [Fact]
        public void VariableUnused()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query variableUnused($atOtherHomes: Boolean) {
                    dog {
                        isHousetrained
                    }
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The following variables were not used: " +
                    "atOtherHomes.", t.Message));
        }

        [Fact]
        public void VariableUsedInFragment()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
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
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void VariableUsedInSecondLevelFragment()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
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
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void VariableUsedInDirective()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query_a = Utf8GraphQLParser.Parse(@"
                query variableUsedInFragment($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedFragment
                    }
                }

                fragment isHousetrainedFragment on Dog {
                    isHousetrained @skip(if: $atOtherHomes)
                }
            ");

            DocumentNode query_b = Utf8GraphQLParser.Parse(@"
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
            context.Prepare(query_a);
            Rule.Validate(context, query_a);

            context.Prepare(query_b);
            Rule.Validate(context, query_b);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void VariableNotUsedWithinFragment()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
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
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The following variables were not used: " +
                    "atOtherHomes.", t.Message));
        }

        [Fact]
        public void QueryWithExtraVar()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
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
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The following variables were not used: " +
                    "extra.", t.Message));
        }

        [Fact]
        public void VariableUsedAndDeclared()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query variableIsDefined($atOtherHomes: Boolean)
                {
                    dog {
                        isHousetrained(atOtherHomes: $atOtherHomes)
                    }
                }");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void VariableUsedInComplexInput()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query queryWithComplexInput($name: String)
                {
                    findDog(complex: { name: $name }) {
                        name
                    }
                }");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void VariableUsedInListInput()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query queryWithListInput($value: Bool)
                {
                    booleanList(booleanListArg: [ $value ])
                }");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void VariableUsedAndNotDeclared()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query variableIsDefined
                {
                    dog {
                        isHousetrained(atOtherHomes: $atOtherHomes)
                    }
                }");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The following variables were not declared: " +
                    "atOtherHomes.", t.Message));
        }

        [Fact]
        public void VariableUsedAndNotDeclared2()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
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
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The following variables were not declared: " +
                    "atOtherHomes.", t.Message));
        }

        [Fact]
        public void VarsMustBeDefinedInAllOperationsInWhichAFragmentIsUsed()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
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
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void VarsMustBeDefinedInAllOperationsInWhichAFragmentIsUsedErr()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
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
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The following variables were not declared: " +
                    "atOtherHomes.", t.Message));
        }
    }
}

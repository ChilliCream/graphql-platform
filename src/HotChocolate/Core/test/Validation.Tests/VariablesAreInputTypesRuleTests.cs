using HotChocolate.Language;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation
{
    public class VariablesAreInputTypesRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public VariablesAreInputTypesRuleTests()
            : base(services => services.AddVariableUniqueAndInputTypeRule())
        {
        }

        [Fact]
        public void QueriesWithValidVariableTypes()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query takesBoolean($atOtherHomes: Boolean) {
                    dog {
                        isHousetrained(atOtherHomes: $atOtherHomes)
                    }
                }

                query takesComplexInput($complexInput: ComplexInput) {
                    findDog(complex: $complexInput) {
                        name
                    }
                }

                query TakesListOfBooleanBang($booleans: [Boolean!]) {
                    booleanList(booleanListArg: $booleans)
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void QueriesWithInvalidVariableTypes()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query takesCat($cat: Cat) {
                    # ...
                }

                query takesDogBang($dog: Dog!) {
                    # ...
                }

                query takesListOfPet($pets: [Pet]) {
                    # ...
                }

                query takesCatOrDog($catOrDog: CatOrDog) {
                    # ...
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.NotEmpty(context.Errors);
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The type of variable `cat` is not an input type.",
                    t.Message),
                t => Assert.Equal(
                    "The type of variable `dog` is not an input type.",
                    t.Message),
                t => Assert.Equal(
                    "The type of variable `pets` is not an input type.",
                    t.Message),
                t => Assert.Equal(
                    "The type of variable `catOrDog` is not an input type.",
                    t.Message));
        }
    }
}

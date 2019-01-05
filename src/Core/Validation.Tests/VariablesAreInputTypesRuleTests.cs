using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class VariablesAreInputTypesRuleTests
        : ValidationTestBase
    {
        public VariablesAreInputTypesRuleTests()
            : base(new VariablesAreInputTypesRule())
        {
        }

        [Fact]
        public void QueriesWithValidVariableTypes()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void QueriesWithInvalidVariableTypes()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
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

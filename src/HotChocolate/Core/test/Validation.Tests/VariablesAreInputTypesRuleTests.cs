using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class VariablesAreInputTypesRuleTests
    : DocumentValidatorVisitorTestBase
{
    public VariablesAreInputTypesRuleTests()
        : base(builder => builder.AddVariableRules())
    {
    }

    [Fact]
    public void QueriesWithValidVariableTypes()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
                query takesBoolean($atOtherHomes: Boolean) {
                    dog {
                        isHouseTrained(atOtherHomes: $atOtherHomes)
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
        ExpectErrors(@"
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
    }
}

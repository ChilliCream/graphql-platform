using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class VariableUniquenessRuleTests
    : DocumentValidatorVisitorTestBase
{
    public VariableUniquenessRuleTests()
        : base(builder => builder.AddVariableRules())
    {
    }

    [Fact]
    public void OperationWithTwoVariablesThatHaveTheSameName()
    {
        // arrange
        var context = ValidationUtils.CreateContext();
        context.MaxAllowedErrors = int.MaxValue;

        var query = Utf8GraphQLParser.Parse(@"
                query houseTrainedQuery($atOtherHomes: Boolean, $atOtherHomes: Boolean) {
                    dog {
                        isHouseTrained(atOtherHomes: $atOtherHomes)
                    }
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Single(context.Errors);
        Assert.Collection(context.Errors,
            t => Assert.Equal(
                "A document containing operations that " +
                "define more than one variable with the same " +
                "name is invalid for execution.", t.Message));
    }

    [Fact]
    public void NoOperationHasVariablesThatShareTheSameName()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
                query ($foo: Boolean = true, $bar: Boolean = false) {
                    dog @skip(if: $foo) {
                        isHouseTrained
                    }
                    dog @skip(if: $bar) {
                        isHouseTrained
                    }
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void TwoOperationsThatShareVariableName()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
                query A($atOtherHomes: Boolean) {
                  ...HouseTrainedFragment
                }

                query B($atOtherHomes: Boolean) {
                  ...HouseTrainedFragment
                }

                fragment HouseTrainedFragment on Query {
                  dog {
                    isHouseTrained(atOtherHomes: $atOtherHomes)
                  }
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Empty(context.Errors);
    }
}

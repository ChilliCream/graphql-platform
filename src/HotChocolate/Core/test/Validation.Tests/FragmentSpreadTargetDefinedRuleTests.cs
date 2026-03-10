using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;

namespace HotChocolate.Validation;

public class FragmentSpreadTargetDefinedRuleTests
    : DocumentValidatorVisitorTestBase
{
    public FragmentSpreadTargetDefinedRuleTests()
        : base(builder => builder.AddFragmentRules())
    {
    }

    [Fact]
    public void UndefinedFragment()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            {
                dog {
                    ...undefinedFragment
                }
            }
            """);

        var context = ValidationUtils.CreateContext(document, maxAllowedErrors: int.MaxValue);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Collection(context.Errors,
            t => Assert.Equal(
                "The specified fragment `undefinedFragment` "
                + "does not exist.",
                t.Message));
        context.Errors.MatchSnapshot();
    }

    [Fact]
    public void DefinedFragment()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            {
                dog {
                    ...definedFragment
                }
            }

            fragment definedFragment on Dog
            {
                barkVolume
            }
            """);

        var context = ValidationUtils.CreateContext(document);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Empty(context.Errors);
    }
}

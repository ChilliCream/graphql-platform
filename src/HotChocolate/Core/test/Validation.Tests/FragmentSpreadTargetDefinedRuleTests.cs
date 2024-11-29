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
        var context = ValidationUtils.CreateContext();
        context.MaxAllowedErrors = int.MaxValue;

        var query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...undefinedFragment
                    }
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Collection(context.Errors,
            t => Assert.Equal(
                "The specified fragment `undefinedFragment` " +
                "does not exist.",
                t.Message));
        context.Errors.MatchSnapshot();
    }

    [Fact]
    public void DefinedFragment()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...definedFragment
                    }
                }

                fragment definedFragment on Dog
                {
                    barkVolume
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Empty(context.Errors);
    }
}

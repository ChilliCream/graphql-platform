using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;

namespace HotChocolate.Validation;

public class FragmentsMustBeUsedRuleTests
    : DocumentValidatorVisitorTestBase
{
    public FragmentsMustBeUsedRuleTests()
        : base(builder => builder.AddFragmentRules())
    {
    }

    [Fact]
    public void UnusedFragment()
    {
        // arrange
        var context = ValidationUtils.CreateContext();
        context.MaxAllowedErrors = int.MaxValue;

        var query = Utf8GraphQLParser.Parse(@"
                fragment nameFragment on Dog { # unused
                    name
                }

                {
                    dog {
                        name
                    }
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Collection(context.Errors,
            t => Assert.Equal(
                "The specified fragment `nameFragment` " +
                "is not used within the current document.", t.Message));
        context.Errors.MatchSnapshot();
    }

    [Fact]
    public void UsedFragment()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
                fragment nameFragment on Dog {
                    name
                }

                {
                    dog {
                        name
                        ... nameFragment
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
    public void UsedNestedFragment()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
                fragment nameFragment on Dog {
                    name
                    ... nestedNameFragment
                }

                fragment nestedNameFragment on Dog {
                    name
                }

                {
                    dog {
                        name
                        ... nameFragment
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

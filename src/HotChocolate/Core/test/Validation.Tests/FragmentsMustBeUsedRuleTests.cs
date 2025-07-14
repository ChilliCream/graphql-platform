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
        var document = Utf8GraphQLParser.Parse(
            """
            fragment nameFragment on Dog { # unused
              name
            }

            {
              dog {
                name
              }
            }
            """);

        var context = ValidationUtils.CreateContext(document, maxAllowedErrors: int.MaxValue);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Collection(context.Errors,
            t => Assert.Equal(
                "The specified fragment `nameFragment` "
                + "is not used within the current document.", t.Message));
        context.Errors.MatchSnapshot();
    }

    [Fact]
    public void UsedFragment()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            fragment nameFragment on Dog {
              name
            }

            {
              dog {
                name
                ... nameFragment
              }
            }
            """);

        var context = ValidationUtils.CreateContext(document);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void UsedNestedFragment()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
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
            """);

        var context = ValidationUtils.CreateContext(document);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Empty(context.Errors);
    }
}

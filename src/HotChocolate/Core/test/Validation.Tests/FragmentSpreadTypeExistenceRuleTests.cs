using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class FragmentSpreadTypeExistenceRuleTests
    : DocumentValidatorVisitorTestBase
{
    public FragmentSpreadTypeExistenceRuleTests()
        : base(builder => builder.AddFragmentRules())
    {
    }

    [Fact]
    public void CorrectTypeOnFragment()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            {
                dog {
                    ...correctType
                }
            }

            fragment correctType on Dog {
                name
            }
            """);

        var context = ValidationUtils.CreateContext(document);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void CorrectTypeOnInlineFragment()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            {
                dog {
                    ...inlineFragment
                }
            }

            fragment inlineFragment on Dog {
                ... on Dog {
                    name
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
    public void CorrectTypeOnInlineFragment2()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            {
                dog {
                    ...inlineFragment2
                }
            }

            fragment inlineFragment2 on Dog {
                ... @include(if: true) {
                    name
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
    public void NotOnExistingTypeOnFragment()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            {
                dog {
                    ...notOnExistingType
                }
            }

            fragment notOnExistingType on NotInSchema {
                name
            }
            """);

        var context = ValidationUtils.CreateContext(document, maxAllowedErrors: int.MaxValue);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Collection(context.Errors,
            t =>
            {
                Assert.Equal(
                    "Unknown type `NotInSchema`.",
                    t.Message);
            });
        context.Errors.MatchSnapshot();
    }

    [Fact]
    public void NotExistingTypeOnInlineFragment()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            {
                dog {
                    ...inlineNotExistingType
                }
            }

            fragment inlineNotExistingType on Dog {
                ... on NotInSchema {
                    name
                }
            }
            """);

        var context = ValidationUtils.CreateContext(document, maxAllowedErrors: int.MaxValue);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Collection(context.Errors,
            t =>
            {
                Assert.Equal(
                    "Unknown type `NotInSchema`.",
                    t.Message);
            });
        context.Errors.MatchSnapshot();
    }
}

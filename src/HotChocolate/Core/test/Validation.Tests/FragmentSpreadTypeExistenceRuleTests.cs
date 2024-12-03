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
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...correctType
                    }
                }

                fragment correctType on Dog {
                    name
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void CorrectTypeOnInlineFragment()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
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
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void CorrectTypeOnInlineFragment2()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
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
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void NotOnExistingTypeOnFragment()
    {
        // arrange
        var context = ValidationUtils.CreateContext();
        context.MaxAllowedErrors = int.MaxValue;

        var query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...notOnExistingType
                    }
                }

                fragment notOnExistingType on NotInSchema {
                    name
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

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
        var context = ValidationUtils.CreateContext();
        context.MaxAllowedErrors = int.MaxValue;

        var query = Utf8GraphQLParser.Parse(@"
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
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

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

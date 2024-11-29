using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class FieldMustBeDefinedRuleTests
    : DocumentValidatorVisitorTestBase
{
    public FieldMustBeDefinedRuleTests()
        : base(builder => builder.AddFieldRules())
    {
    }

    [Fact]
    public void FieldIsNotDefinedOnTypeInFragment()
    {
        // arrange
        var context = ValidationUtils.CreateContext();
        context.MaxAllowedErrors = int.MaxValue;

        var query = Utf8GraphQLParser.Parse(@"
                query {
                    dog {
                        ... fieldNotDefined
                        ... aliasedLyingFieldTargetNotDefined
                    }
                }

                fragment fieldNotDefined on Dog {
                    meowVolume
                }

                fragment aliasedLyingFieldTargetNotDefined on Dog {
                    barkVolume: kawVolume
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Collection(context.Errors,
            t => Assert.Equal(
                "The field `meowVolume` does not exist " +
                "on the type `Dog`.", t.Message),
            t => Assert.Equal(
                "The field `kawVolume` does not exist " +
                "on the type `Dog`.", t.Message));
        context.Errors.MatchSnapshot();
    }

    [Fact]
    public void InterfaceFieldSelectionOnPet()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
                query {
                    dog {
                        ... interfaceFieldSelection
                    }
                }

                fragment interfaceFieldSelection on Pet {
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
    public void DefinedOnImplementorsButNotInterfaceOnPet()
    {
        // arrange
        var context = ValidationUtils.CreateContext();
        context.MaxAllowedErrors = int.MaxValue;

        var query = Utf8GraphQLParser.Parse(@"
                query {
                    dog {
                        ... definedOnImplementorsButNotInterface
                    }
                }

                fragment definedOnImplementorsButNotInterface on Pet {
                    nickname
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Collection(context.Errors,
            t => Assert.Equal(
                "The field `nickname` does not exist " +
                "on the type `Pet`.", t.Message));
        context.Errors.First().MatchSnapshot();
    }

    [Fact]
    public void InDirectFieldSelectionOnUnion()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
                query {
                    dog {
                        ... inDirectFieldSelectionOnUnion
                    }
                }

                fragment inDirectFieldSelectionOnUnion on CatOrDog {
                    __typename
                    ... on Pet {
                        name
                    }
                    ... on Dog {
                        barkVolume
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
    public void DirectFieldSelectionOnUnion()
    {
        // arrange
        var context = ValidationUtils.CreateContext();
        context.MaxAllowedErrors = int.MaxValue;

        var query = Utf8GraphQLParser.Parse(@"
                query {
                    catOrDog {
                        ... directFieldSelectionOnUnion
                    }
                }

                fragment directFieldSelectionOnUnion on CatOrDog {
                    name
                    barkVolume
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Collection(context.Errors,
            t => Assert.Equal(
                "A union type cannot declare a field directly. " +
                "Use inline fragments or fragments instead.", t.Message));
        context.Errors.First().MatchSnapshot();
    }

    [Fact]
    public void IntrospectionFieldsOnInterface()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
                query {
                    dog {
                        ... interfaceFieldSelection
                    }
                }

                fragment interfaceFieldSelection on Pet {
                    __typename
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void IntrospectionFieldsOnUnion()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
                query {
                    dog {
                        ... unionFieldSelection
                    }
                }

                fragment unionFieldSelection on CatOrDog {
                    __typename
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void IntrospectionFieldsOnObject()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();
        var query = Utf8GraphQLParser.Parse(@"
                query {
                    catOrDog {
                        ... unionFieldSelection
                    }
                }

                fragment interfaceFieldSelection on Cat {
                    __typename
                }
            ");
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.Empty(context.Errors);
    }
}

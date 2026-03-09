using HotChocolate.Language;
using HotChocolate.Validation.Rules;

namespace HotChocolate.Validation;

public class FieldMustBeDefinedRuleTests()
    : DocumentValidatorVisitorTestBase(builder => builder.AddRule<FieldSelectionsRule>())
{
    [Fact]
    public void FieldIsNotDefinedOnTypeInFragment()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
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
            """);

        var context = ValidationUtils.CreateContext(document, maxAllowedErrors: int.MaxValue);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Collection(context.Errors,
            t => Assert.Equal(
                "The field `meowVolume` does not exist "
                + "on the type `Dog`.", t.Message),
            t => Assert.Equal(
                "The field `kawVolume` does not exist "
                + "on the type `Dog`.", t.Message));
        var snapshot = new Snapshot();

        foreach (var error in context.Errors)
        {
            snapshot.Add(error);
        }

        snapshot.Match();
    }

    [Fact]
    public void InterfaceFieldSelectionOnPet()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query {
                dog {
                    ... interfaceFieldSelection
                }
            }

            fragment interfaceFieldSelection on Pet {
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
    public void DefinedOnImplementorsButNotInterfaceOnPet()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query {
                dog {
                    ... definedOnImplementorsButNotInterface
                }
            }

            fragment definedOnImplementorsButNotInterface on Pet {
                nickname
            }
            """);
        var context = ValidationUtils.CreateContext(document, maxAllowedErrors: int.MaxValue);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Collection(context.Errors,
            t => Assert.Equal(
                "The field `nickname` does not exist "
                + "on the type `Pet`.", t.Message));
        context.Errors[0].MatchSnapshot();
    }

    [Fact]
    public void InDirectFieldSelectionOnUnion()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
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
            """);
        var context = ValidationUtils.CreateContext(document);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void DirectFieldSelectionOnUnion()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query {
                catOrDog {
                    ... directFieldSelectionOnUnion
                }
            }

            fragment directFieldSelectionOnUnion on CatOrDog {
                name
                barkVolume
            }
            """);
        var context = ValidationUtils.CreateContext(document, maxAllowedErrors: int.MaxValue);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Collection(context.Errors,
            t => Assert.Equal(
                "A union type cannot declare a field directly. "
                + "Use inline fragments or fragments instead.", t.Message));
        context.Errors[0].MatchSnapshot();
    }

    [Fact]
    public void IntrospectionFieldsOnInterface()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query {
                dog {
                    ... interfaceFieldSelection
                }
            }

            fragment interfaceFieldSelection on Pet {
                __typename
            }
            """);
        var context = ValidationUtils.CreateContext(document);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void IntrospectionFieldsOnUnion()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query {
                dog {
                    ... unionFieldSelection
                }
            }

            fragment unionFieldSelection on CatOrDog {
                __typename
            }
            """);
        var context = ValidationUtils.CreateContext(document);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void IntrospectionFieldsOnObject()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            query {
                catOrDog {
                    ... unionFieldSelection
                }
            }

            fragment interfaceFieldSelection on Cat {
                __typename
            }
            """);
        var context = ValidationUtils.CreateContext(document);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Empty(context.Errors);
    }
}

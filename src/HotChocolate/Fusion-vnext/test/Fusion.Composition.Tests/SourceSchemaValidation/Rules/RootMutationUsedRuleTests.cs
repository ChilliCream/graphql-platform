using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

public sealed class RootMutationUsedRuleTests : CompositionTestBase
{
    private readonly SourceSchemaValidator _sourceSchemaValidator =
        new([new RootMutationUsedRule()]);

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _sourceSchemaValidator.Validate(context);

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(context.Log.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(string[] sdl, string[] errorMessages)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _sourceSchemaValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, context.Log.Select(e => e.Message).ToArray());
        Assert.True(context.Log.All(e => e.Code == "ROOT_MUTATION_USED"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Valid example.
            {
                [
                    """
                    schema {
                        mutation: Mutation
                    }

                    type Mutation {
                        createProduct(name: String): Product
                    }

                    type Product {
                        id: ID!
                        name: String
                    }
                    """
                ]
            }
        };
    }

    public static TheoryData<string[], string[]> InvalidExamplesData()
    {
        return new TheoryData<string[], string[]>
        {
            // The following example violates the rule because "RootMutation" is used as the root
            // mutation type, but a type named "Mutation" is also defined.
            {
                [
                    """
                    schema {
                        mutation: RootMutation
                    }

                    type RootMutation {
                        createProduct(name: String): Product
                    }

                    type Mutation {
                        deprecatedField: String
                    }
                    """
                ],
                [
                    "The root mutation type in schema 'A' must be named 'Mutation'."
                ]
            },
            // A type named "Mutation" is not the root mutation type.
            {
                [
                    "scalar Mutation"
                ],
                [
                    "The root mutation type in schema 'A' must be named 'Mutation'."
                ]
            }
        };
    }
}

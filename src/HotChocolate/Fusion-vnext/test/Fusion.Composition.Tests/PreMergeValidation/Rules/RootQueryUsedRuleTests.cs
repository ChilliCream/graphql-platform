using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class RootQueryUsedRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator = new([new RootQueryUsedRule()]);

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _preMergeValidator.Validate(context);

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
        var result = _preMergeValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, context.Log.Select(e => e.Message).ToArray());
        Assert.True(context.Log.All(e => e.Code == "ROOT_QUERY_USED"));
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
                        query: Query
                    }

                    type Query {
                        product(id: ID!): Product
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
            // The following example violates the rule because "RootQuery" is used as the root query
            // type, but a type named "Query" is also defined.
            {
                [
                    """
                    schema {
                        query: RootQuery
                    }

                    type RootQuery {
                        product(id: ID!): Product
                    }

                    type Query {
                        deprecatedField: String
                    }
                    """
                ],
                [
                    "The root query type in schema 'A' must be named 'Query'."
                ]
            },
            // A type named "Query" is not the root query type.
            {
                [
                    "scalar Query"
                ],
                [
                    "The root query type in schema 'A' must be named 'Query'."
                ]
            }
        };
    }
}

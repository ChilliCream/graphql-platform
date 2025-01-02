using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class QueryRootTypeInaccessibleRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator =
        new([new QueryRootTypeInaccessibleRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "QUERY_ROOT_TYPE_INACCESSIBLE"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, no @inaccessible annotation is applied to the query root, so the
            // rule is satisfied.
            {
                [
                    """
                    extend schema {
                        query: Query
                    }

                    type Query {
                        allBooks: [Book]
                    }

                    type Book {
                        id: ID!
                        title: String
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
            // Since the schema marks the query root type as @inaccessible, the rule is violated.
            // QUERY_ROOT_TYPE_INACCESSIBLE is raised because a schema’s root query type cannot be
            // hidden from consumers.
            {
                [
                    """
                    extend schema {
                        query: Query
                    }

                    type Query @inaccessible {
                        allBooks: [Book]
                    }

                    type Book {
                        id: ID!
                        title: String
                    }
                    """
                ],
                [
                    "The root query type in schema 'A' must be accessible."
                ]
            }
        };
    }
}

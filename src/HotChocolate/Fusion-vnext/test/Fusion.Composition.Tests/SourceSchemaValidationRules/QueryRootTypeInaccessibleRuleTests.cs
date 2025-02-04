using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class QueryRootTypeInaccessibleRuleTests : CompositionTestBase
{
    private static readonly object s_rule = new QueryRootTypeInaccessibleRule();
    private static readonly ImmutableArray<object> s_rules = [s_rule];
    private readonly CompositionLog _log = new();

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new SourceSchemaValidator(schemas, s_rules, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(_log.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(string[] sdl, string[] errorMessages)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new SourceSchemaValidator(schemas, s_rules, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, _log.Select(e => e.Message).ToArray());
        Assert.True(_log.All(e => e.Code == "QUERY_ROOT_TYPE_INACCESSIBLE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
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

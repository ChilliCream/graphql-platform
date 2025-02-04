using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class RootQueryUsedRuleTests : CompositionTestBase
{
    private static readonly object s_rule = new RootQueryUsedRule();
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
        Assert.True(_log.All(e => e.Code == "ROOT_QUERY_USED"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
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

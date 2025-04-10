using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class NoQueriesRuleTests
{
    private static readonly object s_rule = new NoQueriesRule();
    private static readonly ImmutableArray<object> s_rules = [s_rule];
    private readonly CompositionLog _log = new();

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var merger = new SourceSchemaMerger(schemas);
        var mergeResult = merger.Merge();
        var validator = new PostMergeValidator(mergeResult.Value, s_rules, schemas, _log);

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
        var merger = new SourceSchemaMerger(schemas);
        var mergeResult = merger.Merge();
        var validator = new PostMergeValidator(mergeResult.Value, s_rules, schemas, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, _log.Select(e => e.Message).ToArray());
        Assert.True(_log.All(e => e.Code == "NO_QUERIES"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, at least one schema provides accessible query fields, satisfying the
            // rule.
            {
                [
                    """
                    # Schema A
                    type Query {
                        product(id: ID!): Product
                    }

                    type Product {
                        id: ID!
                    }
                    """,
                    """
                    # Schema B
                    type Query {
                        review(id: ID!): Review
                    }

                    type Review {
                        id: ID!
                        content: String
                        rating: Int
                    }
                    """
                ]
            },
            // Even if some query fields are marked as @inaccessible, as long as there is at least
            // one accessible query field in the composed schema, the rule is satisfied.
            {
                [
                    """
                    # Schema A
                    type Query {
                        internalData: InternalData @inaccessible
                    }

                    type InternalData {
                        secret: String
                    }
                    """,
                    """
                    # Schema B
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
            // If all query fields in all schemas are marked as @inaccessible, the composed schema
            // will lack accessible query fields, violating the rule.
            {
                [
                    """
                    # Schema A
                    type Query {
                        internalData: InternalData @inaccessible
                    }

                    type InternalData {
                        secret: String
                    }
                    """,
                    """
                    # Schema B
                    type Query {
                        adminStats: AdminStats @inaccessible
                    }

                    type AdminStats {
                        userCount: Int
                    }
                    """
                ],
                [
                    "The merged query type has no accessible fields."
                ]
            }
        };
    }
}

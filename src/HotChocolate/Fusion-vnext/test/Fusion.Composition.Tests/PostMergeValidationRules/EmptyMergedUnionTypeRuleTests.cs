using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EmptyMergedUnionTypeRuleTests : CompositionTestBase
{
    private static readonly object s_rule = new EmptyMergedUnionTypeRule();
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
        Assert.True(_log.All(e => e.Code == "EMPTY_MERGED_UNION_TYPE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the merged union type "SearchResult" is valid. It includes
            // all member types from both source schemas, with "User" being hidden due to the
            // @inaccessible directive in one of the source schemas.
            {
                [
                    """
                    # Schema A
                    union SearchResult = User | Product

                    type User @inaccessible {
                        id: ID!
                    }

                    type Product {
                        id: ID!
                    }
                    """,
                    """
                    # Schema B
                    union SearchResult = Product | Order

                    type Product {
                        id: ID!
                    }

                    type Order {
                        id: ID!
                    }
                    """
                ]
            },
            // If the @inaccessible directive is applied to a union type itself, the entire merged
            // union type is excluded from the composite execution schema, and it is not required to
            // contain any members.
            {
                [
                    """
                    # Schema A
                    union SearchResult @inaccessible = User | Product

                    type User {
                        id: ID!
                    }

                    type Product {
                        id: ID!
                    }
                    """,
                    """
                    # Schema B
                    union SearchResult = Product | Order

                    type Product {
                        id: ID!
                    }

                    type Order {
                        id: ID!
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
            // This example demonstrates an invalid merged union type. In this case, "SearchResult"
            // is defined in two source schemas, but all member types are marked as @inaccessible in
            // at least one of the source schemas, resulting in an empty merged union type.
            {
                [
                    """
                    # Schema A
                    union SearchResult = User | Product

                    type User @inaccessible {
                        id: ID!
                    }

                    type Product {
                        id: ID!
                    }
                    """,
                    """
                    # Schema B
                    union SearchResult = User | Product

                    type User {
                        id: ID!
                    }

                    type Product @inaccessible {
                        id: ID!
                    }
                    """
                ],
                [
                    "The merged union type 'SearchResult' is empty."
                ]
            }
        };
    }
}

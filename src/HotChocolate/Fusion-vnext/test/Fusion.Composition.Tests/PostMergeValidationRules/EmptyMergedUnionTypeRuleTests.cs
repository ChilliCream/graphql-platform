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
            // TODO: Use examples from spec
            {
                [
                    """
                    # Schema A
                    type A {
                        name: String
                    }

                    type B {
                        id: Int!
                    }

                    union C = A | B
                    """,
                    """
                    # Schema B
                    type A {
                        name: String
                    }

                    union C = A
                    """
                ]
            },
            // If the @inaccessible directive is applied to a union type itself, the entire merged
            // union type is excluded from the composite schema, and it is not required to contain
            // any types.
            {
                [
                    """
                    # Schema A
                    type A {
                        name: String
                    }

                    type B {
                        id: Int!
                    }

                    union C @inaccessible = A | B
                    """,
                    """
                    # Schema B
                    type A {
                        name: String
                    }

                    union C = A
                    """
                ]
            }
        };
    }

    public static TheoryData<string[], string[]> InvalidExamplesData()
    {
        return new TheoryData<string[], string[]>
        {
            // This example demonstrates an invalid merged union type. In this case, "C" is defined
            // in two source schemas, but all member types are marked as @inaccessible in at least
            // one of the source schemas, resulting in an empty merged union type.
            {
                [
                    """
                    # Schema A
                    type A {
                        name: String
                    }

                    type B @inaccessible {
                        id: Int!
                    }

                    union C = A | B
                    """,
                    """
                    # Schema B
                    type A @inaccessible {
                        name: String
                    }

                    union C = A
                    """
                ],
                [
                    "The merged union type 'C' is empty."
                ]
            }
        };
    }
}

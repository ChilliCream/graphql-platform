using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EmptyMergedInputObjectTypeRuleTests
{
    private static readonly object s_rule = new EmptyMergedInputObjectTypeRule();
    private static readonly ImmutableArray<object> s_rules = [s_rule];
    private readonly CompositionLog _log = new();

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var merger = new SourceSchemaMerger(
            schemas,
            new SourceSchemaMergerOptions { RemoveUnreferencedTypes = false });
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
        var merger = new SourceSchemaMerger(
            schemas,
            new SourceSchemaMergerOptions { RemoveUnreferencedTypes = false });
        var mergeResult = merger.Merge();
        var validator = new PostMergeValidator(mergeResult.Value, s_rules, schemas, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, _log.Select(e => e.Message).ToArray());
        Assert.True(_log.All(e => e.Code == "EMPTY_MERGED_INPUT_OBJECT_TYPE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the merged input object type "BookFilter" is valid.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        name: String
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        name: String
                    }
                    """
                ]
            },
            // If the @inaccessible directive is applied to an input object type itself, the entire
            // merged input object type is excluded from the composite schema, and it is not
            // required to contain any fields.
            {
                [
                    """
                    # Schema A
                    input BookFilter @inaccessible {
                        name: String
                        minPageCount: Int
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
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
            // This example demonstrates an invalid merged input object type. In this case,
            // "BookFilter" is defined in two source schemas, but all fields are marked as
            // @inaccessible in at least one of the source schemas, resulting in an empty merged
            // input object type.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        name: String @inaccessible
                        paperback: Boolean
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        name: String
                        paperback: Boolean @inaccessible
                    }
                    """
                ],
                [
                    "The merged input object type 'BookFilter' is empty."
                ]
            },
            // This example demonstrates where the merged input object type is empty because no
            // fields intersect between the two source schemas.
            {
                [
                    """
                    # Schema A
                    input BookFilter {
                        paperback: Boolean
                    }
                    """,
                    """
                    # Schema B
                    input BookFilter {
                        name: String
                    }
                    """
                ],
                [
                    "The merged input object type 'BookFilter' is empty."
                ]
            }
        };
    }
}

using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EmptyMergedEnumTypeRuleTests : CompositionTestBase
{
    private static readonly object s_rule = new EmptyMergedEnumTypeRule();
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
        Assert.True(_log.All(e => e.Code == "EMPTY_MERGED_ENUM_TYPE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the merged enum type "DeliveryStatus" is valid. It includes
            // all values from both source schemas, with PENDING being hidden due to the
            // @inaccessible directive in one of the source schemas.
            {
                [
                    """
                    # Schema A
                    enum DeliveryStatus {
                        PENDING @inaccessible
                        SHIPPED
                        DELIVERED
                    }
                    """,
                    """
                    # Schema B
                    enum DeliveryStatus {
                        SHIPPED
                        DELIVERED
                    }
                    """
                ]
            },
            // If the @inaccessible directive is applied to an enum type itself, the entire merged
            // enum type is excluded from the composite execution schema, and it is not required to
            // contain any values.
            {
                [
                    """
                    # Schema A
                    enum DeliveryStatus @inaccessible {
                        SHIPPED
                        DELIVERED
                    }
                    """,
                    """
                    # Schema B
                    enum DeliveryStatus {
                        SHIPPED
                        DELIVERED
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
            // This example demonstrates an invalid merged enum type. In this case, "DeliveryStatus"
            // is defined in two source schemas, but all values are marked as @inaccessible in at
            // least one of the source schemas, resulting in an empty merged enum type.
            {
                [
                    """
                    # Schema A
                    enum DeliveryStatus {
                        PENDING @inaccessible
                        DELIVERED
                    }
                    """,
                    """
                    # Schema B
                    enum DeliveryStatus {
                        PENDING
                        DELIVERED @inaccessible
                    }
                    """
                ],
                [
                    "The merged enum type 'DeliveryStatus' is empty."
                ]
            }
        };
    }
}

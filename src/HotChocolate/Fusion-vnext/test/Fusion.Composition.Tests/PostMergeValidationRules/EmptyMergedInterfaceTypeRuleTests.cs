using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EmptyMergedInterfaceTypeRuleTests : CompositionTestBase
{
    private static readonly object s_rule = new EmptyMergedInterfaceTypeRule();
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
        Assert.True(_log.All(e => e.Code == "EMPTY_MERGED_INTERFACE_TYPE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the merged object type "Product" is valid. It includes all
            // fields from both source schemas, with "price" being hidden due to the @inaccessible
            // directive in one of the source schemas.
            {
                [
                    """
                    # Schema A
                    interface Product {
                        name: String
                        price: Int @inaccessible
                    }
                    """,
                    """
                    # Schema B
                    interface Product {
                        name: String
                        inStock: Boolean
                    }
                    """
                ]
            },
            // If the @inaccessible directive is applied to an interface type itself, the entire
            // merged interface type is excluded from the composite execution schema, and it is not
            // required to contain any fields.
            {
                [
                    """
                    # Schema A
                    interface Product @inaccessible {
                        name: String
                        price: Int
                    }
                    """,
                    """
                    # Schema B
                    interface Product {
                        name: String
                        inStock: Boolean
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
            // This example demonstrates an invalid merged interface type. In this case, "Product"
            // is defined in two source schemas, but all fields are marked as @inaccessible in at
            // least one of the source schemas, resulting in an empty merged interface type.
            {
                [
                    """
                    # Schema A
                    interface Product {
                        name: String
                        price: Int @inaccessible
                    }
                    """,
                    """
                    # Schema B
                    interface Product {
                        name: String @inaccessible
                        price: Int
                    }
                    """
                ],
                [
                    "The merged interface type 'Product' is empty."
                ]
            }
        };
    }
}

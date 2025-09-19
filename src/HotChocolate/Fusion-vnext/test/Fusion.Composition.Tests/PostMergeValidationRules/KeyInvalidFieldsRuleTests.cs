using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class KeyInvalidFieldsRuleTests
{
    private static readonly object s_rule = new KeyInvalidFieldsRule();
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
        Assert.Equal(errorMessages, _log.Select(e => e.Message.ReplaceLineEndings("\n")).ToArray());
        Assert.True(_log.All(e => e.Code == "KEY_INVALID_FIELDS"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the "fields" argument of the @key directive is properly defined with
            // valid syntax and references existing fields.
            {
                [
                    """
                    type Product @key(fields: "sku featuredItem { id }") {
                        sku: String!
                        featuredItem: Node!
                    }

                    interface Node {
                        id: ID!
                    }
                    """
                ]
            },
            // In this example, the "fields" argument references a field from another source schema.
            {
                [
                    """
                    type Product @key(fields: "sku featuredItem { id }") {
                        id: ID!
                        sku: String!
                    }
                    """,
                    """
                    type Query {
                        node(id: ID!): Node
                    }

                    type Product {
                        featuredItem: Node!
                    }

                    interface Node {
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
            // In this example, the "fields" argument of the @key directive references a field "id",
            // which does not exist on the "Product" type.
            {
                [
                    """
                    #tmp
                    type Query {
                        productById(id: ID!): Product @lookup
                    }

                    type Product @key(fields: "id") {
                        sku: String!
                    }
                    """
                ],
                [
                    """
                    A @key directive on type 'Product' in schema 'A' specifies an invalid field selection against the composed schema.
                    - The field 'id' does not exist on the type 'Product'.
                    """
                ]
            },
            // Two errors.
            {
                [
                    """
                    type Product @key(fields: "id name") {
                        sku: String!
                    }
                    """
                ],
                [
                    """
                    A @key directive on type 'Product' in schema 'A' specifies an invalid field selection against the composed schema.
                    - The field 'id' does not exist on the type 'Product'.
                    - The field 'name' does not exist on the type 'Product'.
                    """
                ]
            }
        };
    }
}

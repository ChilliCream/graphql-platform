using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class RequireInvalidFieldsRuleTests
{
    private static readonly object s_rule = new RequireInvalidFieldsRule();
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
        Assert.True(_log.All(e => e.Code == "REQUIRE_INVALID_FIELDS"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the @require directive’s "field" argument is a valid
            // selection set and satisfies the rule.
            {
                [
                    """
                    # Schema A
                    type User @key(fields: "id") {
                        id: ID!
                        profile(name: String @require(field: "name")): Profile
                    }

                    type Profile {
                        id: ID!
                        name: String
                    }
                    """,
                    """
                    # Schema B
                    type User @key(fields: "id") {
                        id: ID!
                        name: String
                    }
                    """
                ]
            },
            {
                [
                    """
                    type Product {
                        id: ID!
                        delivery(
                            zip: String!
                            dimension: ProductDimensionInput!
                                @require(field: "{ productSize: dimension.size, productWeight: dimension.weight }")
                        ): DeliveryEstimates
                    }

                    input ProductDimensionInput {
                        productSize: Int!
                        productWeight: Int!
                    }
                    """,
                    """
                    type Product {
                        dimension: ProductDimension!
                    }

                    type ProductDimension {
                        size: Int!
                        weight: Int!
                    }
                    """
                ]
            },
            // In the following example, the @require directive’s "field" argument references a field with a list type.
            {
                [
                    """
                    type CartDiscount {
                        channels(channelIds: [ID!]! @require(field: "channelIds")): ChannelConnection!
                        id: ID!
                    }
                    """,
                    """
                    type CartDiscount {
                       id: ID!
                       name: String!
                       channelIds: [ID!]!
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
            // In this example, the @require directive references a field ("unknownField") that does
            // not exist on the parent type ("Book"), causing a REQUIRE_INVALID_FIELDS error.
            {
                [
                    """
                    type Book {
                        id: ID!
                        pages(pageSize: Int @require(field: "unknownField")): Int
                    }
                    """
                ],
                [
                    "The @require directive on argument 'Book.pages(pageSize:)' in schema 'A' "
                    + "specifies an invalid field selection against the composed schema."
                ]
            },
            // In the following example, the @require directive references a field from itself
            // (Book.size) which is not allowed. This results in a REQUIRE_INVALID_FIELDS error.
            {
                [
                    """
                    type Book {
                        id: ID!
                        size: Int
                        pages(pageSize: Int @require(field: "size")): Int
                    }
                    """
                ],
                [
                    "The @require directive on argument 'Book.pages(pageSize:)' in schema 'A' "
                    + "specifies an invalid field selection against the composed schema."
                ]
            },
            // Referencing a field that exists in both the current schema and another schema.
            {
                [
                    """
                    type Book {
                        id: ID!
                        size: Int
                        pages(pageSize: Int @require(field: "size")): Int
                    }
                    """,
                    """
                    type Book {
                        id: ID!
                        size: Int
                    }
                    """
                ],
                [
                    "The @require directive on argument 'Book.pages(pageSize:)' in schema 'A' "
                    + "specifies an invalid field selection against the composed schema."
                ]
            }
        };
    }
}

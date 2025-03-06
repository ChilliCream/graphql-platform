using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class RequireInvalidFieldsRuleTests : CompositionTestBase
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
                    type User @key(fields: "id") {
                        id: ID!
                        name: String!
                        profile(name: String! @require(field: "name")): Profile
                    }

                    type Profile {
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
                    "The @require directive on argument 'Book.pages(pageSize:)' in schema 'A' " +
                    "specifies an invalid field selection against the composed schema."
                ]
            }
        };
    }
}

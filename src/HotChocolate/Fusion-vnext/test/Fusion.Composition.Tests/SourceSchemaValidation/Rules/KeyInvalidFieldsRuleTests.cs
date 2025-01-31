using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

public sealed class KeyInvalidFieldsRuleTests : CompositionTestBase
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
                    type Product @key(fields: "id") {
                        sku: String!
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' references field " +
                    "'Product.id', which does not exist."
                ]
            },
            // Nested field.
            {
                [
                    """
                    type Product @key(fields: "info { category { id } }") {
                        info: ProductInfo!
                    }

                    type ProductInfo {
                        subcategory: Category
                    }

                    type Category {
                        name: String!
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' references field " +
                    "'ProductInfo.category', which does not exist."
                ]
            },
            // Multiple nested fields.
            {
                [
                    """
                    type Product @key(fields: "category { id name } info { id }") {
                        category: ProductCategory!
                    }

                    type ProductCategory {
                        description: String
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' references field " +
                    "'ProductCategory.id', which does not exist.",

                    "A @key directive on type 'Product' in schema 'A' references field " +
                    "'ProductCategory.name', which does not exist.",

                    "A @key directive on type 'Product' in schema 'A' references field " +
                    "'Product.info', which does not exist."
                ]
            },
            // Multiple keys.
            {
                [
                    """
                    type Product @key(fields: "id") @key(fields: "name") {
                        sku: String!
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' references field " +
                    "'Product.id', which does not exist.",

                    "A @key directive on type 'Product' in schema 'A' references field " +
                    "'Product.name', which does not exist."
                ]
            }
        };
    }
}

using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class KeyFieldsSelectInvalidTypeRuleTests
{
    private static readonly object s_rule = new KeyFieldsSelectInvalidTypeRule();
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
        Assert.True(_log.All(e => e.Code == "KEY_FIELDS_SELECT_INVALID_TYPE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the "Product" type has a valid @key directive referencing the scalar
            // field "sku".
            {
                [
                    """
                    type Product @key(fields: "sku") {
                        sku: String!
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
            // In the following example, the "Product" type has an invalid @key directive
            // referencing a field ("featuredItem") whose type is an interface, violating the rule.
            {
                [
                    """
                    type Product @key(fields: "featuredItem { id }") {
                        featuredItem: Node!
                        sku: String!
                    }

                    interface Node {
                        id: ID!
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' references field "
                    + "'Product.featuredItem', which must not be a list, interface, or union type."
                ]
            },
            // In this example, the @key directive references a field ("tags") of type "List", which
            // is also not allowed.
            {
                [
                    """
                    type Product @key(fields: "tags") {
                        tags: [String!]!
                        sku: String!
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' references field "
                    + "'Product.tags', which must not be a list, interface, or union type."
                ]
            },
            // In this example, the @key directive references a field ("relatedItems") of type
            // "Union", which violates the rule.
            {
                [
                    """
                    type Product @key(fields: "relatedItems") {
                        relatedItems: Related!
                        sku: String!
                    }

                    union Related = Product | Service

                    type Service {
                        id: ID!
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' references field "
                    + "'Product.relatedItems', which must not be a list, interface, or union type."
                ]
            },
            // Nested interface.
            {
                [
                    """
                    type Product @key(fields: "info { featuredItem { id } }") {
                        info: ProductInfo!
                    }

                    type ProductInfo {
                        featuredItem: Node!
                    }

                    interface Node {
                        id: ID!
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' references field "
                    + "'ProductInfo.featuredItem', which must not be a list, interface, or union "
                    + "type."
                ]
            },
            // Nested list.
            {
                [
                    """
                    type Product @key(fields: "info { tags }") {
                        info: ProductInfo!
                    }

                    type ProductInfo {
                        tags: [String!]!
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' references field "
                    + "'ProductInfo.tags', which must not be a list, interface, or union type."
                ]
            },
            // Nested union.
            {
                [
                    """
                    type Product @key(fields: "info { relatedItems }") {
                        info: ProductInfo!
                    }

                    type ProductInfo {
                        relatedItems: Related!
                    }

                    union Related = Product | Service

                    type Service {
                        id: ID!
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' references field "
                    + "'ProductInfo.relatedItems', which must not be a list, interface, or union "
                    + "type."
                ]
            },
            // Multiple keys.
            {
                [
                    """
                    type Product
                        @key(fields: "featuredItem { id }")
                        @key(fields: "tags")
                        @key(fields: "relatedItems") {
                        featuredItem: Node!
                        tags: [String!]!
                        relatedItems: Related!
                    }

                    interface Node {
                        id: ID!
                    }

                    union Related = Product | Service

                    type Service {
                        id: ID!
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' references field "
                    + "'Product.featuredItem', which must not be a list, interface, or union type.",

                    "A @key directive on type 'Product' in schema 'A' references field "
                    + "'Product.tags', which must not be a list, interface, or union type.",

                    "A @key directive on type 'Product' in schema 'A' references field "
                    + "'Product.relatedItems', which must not be a list, interface, or union type."
                ]
            }
        };
    }
}

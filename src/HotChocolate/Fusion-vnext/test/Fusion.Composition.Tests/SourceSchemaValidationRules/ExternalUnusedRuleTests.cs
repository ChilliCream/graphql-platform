using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ExternalUnusedRuleTests
{
    private static readonly object s_rule = new ExternalUnusedRule();
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
        Assert.True(_log.All(e => e.Code == "EXTERNAL_UNUSED"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the "name" field is marked with @external and is used by the
            // @provides directive, satisfying the rule.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        id: ID
                        name: String @external
                    }

                    type Query {
                        productByName(name: String): Product @provides(fields: "name")
                    }
                    """
                ]
            },
            // Provides two fields.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        id: ID
                        name: String @external
                    }

                    type Query {
                        productByName(name: String): Product @provides(fields: "id name")
                    }
                    """
                ]
            },
            // From https://graphql.github.io/composite-schemas-spec/draft/#sec--provides.
            {
                [
                    """
                    # Source schema A
                    type Review {
                        id: ID!
                        product: Product @provides(fields: "sku variation { size }")
                    }

                    type Product @key(fields: "sku variation { id }") {
                        sku: String! @external
                        variation: ProductVariation!
                        name: String!
                    }

                    type ProductVariation {
                        id: String!
                        size: String! @external
                    }
                    """
                ]
            },
            {
                [
                    """"
                    # Source schema A
                    type Review {
                        id: ID!
                        # The @provides directive tells us that this source schema can supply
                        # different fields depending on which concrete type of Product is returned.
                        product: Product
                            @provides(
                                fields: """
                                ... on Book { author }
                                ... on Clothing { size }
                                """
                            )
                    }

                    interface Product @key(fields: "id") {
                        id: ID!
                    }

                    type Book implements Product {
                        id: ID!
                        title: String!
                        author: String! @external
                    }

                    type Clothing implements Product {
                        id: ID!
                        name: String!
                        size: String! @external
                    }

                    type Query {
                        reviews: [Review!]!
                    }
                    """"
                ]
            }
        };
    }

    public static TheoryData<string[], string[]> InvalidExamplesData()
    {
        return new TheoryData<string[], string[]>
        {
            // In this example, the "name" field is marked with @external but is not used by a
            // @provides directive, violating the rule.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        id: ID
                        name: String @external
                    }
                    """
                ],
                [
                    "The external field 'Product.name' in schema 'A' is not referenced by a "
                    + "@provides directive in the schema."
                ]
            },
            // Provides different field.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        title: String @external
                        author: Author
                    }

                    type Query {
                        productByName(name: String): Product @provides(fields: "author")
                    }
                    """
                ],
                [
                    "The external field 'Product.title' in schema 'A' is not referenced by a "
                    + "@provides directive in the schema."
                ]
            },
            // Subselections.
            {
                [
                    """
                    # Source schema A
                    type Review {
                        id: ID!
                        product: Product @provides(fields: "name variation { id }")
                    }

                    type Product @key(fields: "sku variation { id }") {
                        sku: String! @external
                        variation: ProductVariation!
                        name: String!
                    }

                    type ProductVariation {
                        id: String!
                        size: String! @external
                    }
                    """
                ],
                [
                    "The external field 'Product.sku' in schema 'A' is not referenced by a "
                    + "@provides directive in the schema.",

                    "The external field 'ProductVariation.size' in schema 'A' is not referenced "
                    + "by a @provides directive in the schema."
                ]
            },
            // Fragments.
            {
                [
                    """"
                    # Source schema A
                    type Review {
                        id: ID!
                        # The @provides directive tells us that this source schema can supply
                        # different fields depending on which concrete type of Product is returned.
                        product: Product
                            @provides(
                                fields: """
                                ... on Book { id }
                                ... on Clothing { id }
                                """
                            )
                    }

                    interface Product @key(fields: "id") {
                        id: ID!
                    }

                    type Book implements Product {
                        id: ID!
                        title: String!
                        author: String! @external
                    }

                    type Clothing implements Product {
                        id: ID!
                        name: String!
                        size: String! @external
                    }

                    type Query {
                        reviews: [Review!]!
                    }
                    """"
                ],
                [
                    "The external field 'Book.author' in schema 'A' is not referenced by a "
                    + "@provides directive in the schema.",

                    "The external field 'Clothing.size' in schema 'A' is not referenced "
                    + "by a @provides directive in the schema."
                ]
            }
        };
    }
}

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ExternalUnusedRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ExternalUnusedRule();

    // In this example, the "name" field is marked with @external and is used by the @provides
    // directive, satisfying the rule.
    [Fact]
    public void Validate_ExternalUsedInProvides_Succeeds()
    {
        AssertValid(
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
        ]);
    }

    // Provides two fields.
    [Fact]
    public void Validate_ExternalUsedInProvidesTwoFields_Succeeds()
    {
        AssertValid(
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
        ]);
    }

    // From https://graphql.github.io/composite-schemas-spec/draft/#sec--provides.
    [Fact]
    public void Validate_ExternalUsedInProvidesMultipleFields_Succeeds()
    {
        AssertValid(
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
        ]);
    }

    // From https://graphql.github.io/composite-schemas-spec/draft/#sec--provides.
    [Fact]
    public void Validate_ExternalUsedInProvidesWithTypeConditions_Succeeds()
    {
        AssertValid(
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
        ]);
    }

    // In this example, the "name" field is marked with @external but is not used by a @provides
    // directive, violating the rule.
    [Fact]
    public void Validate_ExternalUnused_Fails()
    {
        AssertInvalid(
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
                "The external field 'Product.name' in schema 'A' is not referenced by a @provides "
                + "directive in the schema."
            ]);
    }

    // Provides different field.
    [Fact]
    public void Validate_ExternalUnusedProvidesDifferentField_Fails()
    {
        AssertInvalid(
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
                "The external field 'Product.title' in schema 'A' is not referenced by a @provides "
                + "directive in the schema."
            ]);
    }

    // Subselections.
    [Fact]
    public void Validate_ExternalUnusedProvidesSubselections_Fails()
    {
        AssertInvalid(
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
                "The external field 'Product.sku' in schema 'A' is not referenced by a @provides "
                + "directive in the schema.",

                "The external field 'ProductVariation.size' in schema 'A' is not referenced by a "
                + "@provides directive in the schema."
            ]);
    }

    // Fragments.
    [Fact]
    public void Validate_ExternalUnusedProvidesFragments_Fails()
    {
        AssertInvalid(
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
                "The external field 'Book.author' in schema 'A' is not referenced by a @provides "
                + "directive in the schema.",

                "The external field 'Clothing.size' in schema 'A' is not referenced by a @provides "
                + "directive in the schema."
            ]);
    }
}

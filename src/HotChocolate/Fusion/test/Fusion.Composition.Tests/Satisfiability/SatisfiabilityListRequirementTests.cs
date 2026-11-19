using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityListRequirementTests
{
    [Theory]
    [InlineData("[Variant!]!")]
    [InlineData("[[Variant]]")]
    public void Compose_Should_Fail_When_OnlyProductHasLookupAboveSharedList(string variantsType)
    {
        // arrange
        var sources = CreateSources(variantsType, variantLookup: false);

        // act
        var (result, log) = ComposeSourceSchemas(sources);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, entry => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, entry.Code));
        log.Select(entry => entry.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Variant.shippingEstimate' on path 'A:Query.products<Product> -> A:Product.variants<Variant>'.
              Unable to satisfy the requirement '{ weight }' on field 'B:Variant.shippingEstimate<Int>'.
                The field 'Variant.shippingEstimate' in schema 'B' requires data from another schema, but schema 'B' has no lookup for type 'Variant' that could resolve the field once the required data has been fetched.
            """,
            """
            Unable to access the field 'Variant.shippingEstimate' on path 'A:Query.products<Product> -> B:Product.variants<Variant>'.
              Unable to satisfy the requirement '{ weight }' on field 'B:Variant.shippingEstimate<Int>'.
                The field 'Variant.shippingEstimate' in schema 'B' requires data from another schema, but schema 'B' has no lookup for type 'Variant' that could resolve the field once the required data has been fetched.
            """
        ]);
    }

    [Theory]
    [InlineData("Variant", false)]
    [InlineData("[Variant!]!", true)]
    public void Compose_Should_Succeed_When_LookupCanBindEachRequirement(
        string variantsType,
        bool variantLookup)
    {
        // arrange
        var sources = CreateSources(variantsType, variantLookup);

        // act
        var (result, log) = ComposeSourceSchemas(sources);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData("featured: Variant", "variants: [Variant!]!")]
    [InlineData("variants: [Variant!]!", "featured: Variant")]
    public void Compose_Should_Fail_When_OnlyOnePathToNestedRequirementCrossesList(
        string firstField,
        string secondField)
    {
        // arrange
        SourceSchemaText[] sources =
        [
            new SourceSchemaText(
                "A",
                $$"""
                type Query { product: Product }
                type Product {
                    id: ID! @shareable
                    {{firstField}} @shareable
                    {{secondField}} @shareable
                }
                type Variant { details: Details @shareable }
                type Details { weight: Int! }
                """),
            new SourceSchemaText(
                "B",
                $$"""
                type Query { productById(id: ID!): Product @lookup @internal }
                type Product {
                    id: ID! @shareable
                    {{firstField}} @shareable
                    {{secondField}} @shareable
                }
                type Variant { details: Details @shareable }
                type Details { estimate(weight: Int! @require(field: "weight")): Int! }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sources);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, entry => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, entry.Code));
        log.Select(entry => entry.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Details.estimate' on path 'A:Query.product<Product> -> A:Product.variants<Variant> -> A:Variant.details<Details>'.
              Unable to satisfy the requirement '{ weight }' on field 'B:Details.estimate<Int>'.
                The field 'Details.estimate' in schema 'B' requires data from another schema, but schema 'B' has no lookup for type 'Details' that could resolve the field once the required data has been fetched.
            """,
            """
            Unable to access the field 'Details.estimate' on path 'A:Query.product<Product> -> A:Product.variants<Variant> -> B:Variant.details<Details>'.
              Unable to satisfy the requirement '{ weight }' on field 'B:Details.estimate<Int>'.
                The field 'Details.estimate' in schema 'B' requires data from another schema, but schema 'B' has no lookup for type 'Details' that could resolve the field once the required data has been fetched.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_SharedRootAndEntityListsSplitItemFields()
    {
        // arrange
        const string product =
            """
            type Product {
                id: ID! @shareable
                viewers: [Viewer!]! @shareable
            }
            """;
        SourceSchemaText[] sources =
        [
            new SourceSchemaText(
                "A",
                $$"""
                type Query {
                    products: [Product!]!
                    viewers: [Viewer!]! @shareable
                }
                {{product}}
                type Viewer { name: String }
                """),
            new SourceSchemaText(
                "B",
                $$"""
                type Query {
                    productById(id: ID!): Product @lookup @internal
                    viewers: [Viewer!]! @shareable
                }
                {{product}}
                type Viewer { handle: String }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sources);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Compose_Should_CheckListItemLookup_When_AnotherFieldRequiresHiddenEstimate(bool hasLookup)
    {
        // arrange
        var lookup = hasLookup ? "variantById(id: Int!): Variant @lookup @internal" : "";
        SourceSchemaText[] sources =
        [
            new SourceSchemaText(
                "A",
                """
                type Query { products: [Product!]! }
                type Product {
                    id: Int! @shareable
                    featured: Variant! @shareable
                    variants: [Variant!]! @shareable
                }
                type Variant {
                    id: Int! @shareable
                    weight: Int!
                }
                """),
            new SourceSchemaText(
                "B",
                $$"""
                type Query {
                    productById(id: Int!): Product @lookup @internal
                    {{lookup}}
                }
                type Product {
                    id: Int! @shareable
                    featured: Variant! @shareable
                    variants: [Variant!]! @shareable
                }
                type Variant {
                    id: Int! @shareable
                    estimate(weight: Int! @require(field: "weight")): Int! @inaccessible
                }
                """),
            new SourceSchemaText(
                "C",
                """
                type Query { productById(id: Int!): Product @lookup @internal }
                type Product {
                    id: Int! @shareable
                    total(
                        featured: Int! @require(field: "featured.estimate")
                        estimates: [Int!]! @require(field: "variants[estimate]")
                    ): Int!
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sources);

        // assert
        Assert.Equal(hasLookup, result.IsSuccess);
        Assert.Equal(
            hasLookup ? [] : [LogEntryCodes.UnsatisfiableQueryPath],
            log.Select(entry => entry.Code));
    }

    private static SourceSchemaText[] CreateSources(string variantsType, bool variantLookup)
    {
        var lookup = variantLookup ? "variantById(id: Int!): Variant @lookup @internal" : "";

        return
        [
            new SourceSchemaText(
                "A",
                $$"""
                type Query { products: [Product!]! }
                type Product {
                    id: Int! @shareable
                    variants: {{variantsType}} @shareable
                }
                type Variant {
                    id: Int! @shareable
                    weight: Int!
                }
                """),
            new SourceSchemaText(
                "B",
                $$"""
                type Query {
                    productById(id: Int!): Product @lookup @internal
                    {{lookup}}
                }
                type Product {
                    id: Int! @shareable
                    variants: {{variantsType}} @shareable
                }
                type Variant {
                    id: Int! @shareable
                    shippingEstimate(weight: Int! @require(field: "weight")): Int!
                }
                """)
        ];
    }
}

using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityOverrideTests
{
    [Fact]
    public void Validate_Should_Succeed_When_OverriddenFieldIsResolvedByOverridingSchema()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    products: [Product]
                    productById(id: ID!): Product @lookup @internal
                }

                type Product {
                    id: ID! @shareable
                    name: String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    productById(id: ID!): Product @lookup @internal
                }

                type Product {
                    id: ID! @shareable
                    name: String @override(from: "A")
                    price: Int
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Validate_Should_Fail_When_OverriddenKeyFieldIsNoLongerProvidedByOriginalSchema()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    products: [Product]
                    productById(id: ID!): Product @lookup @internal
                }

                type Product {
                    id: ID!
                    name: String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    productById(id: ID!): Product @lookup @internal
                }

                type Product {
                    id: ID! @override(from: "A")
                    price: Int
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.id' on path 'A:Query.products<Product>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.id<ID>'.
                Unable to satisfy the requirement '{ id }' for lookup 'productById' in schema 'B'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Product.id' on path 'A:Query.products<Product>'.
                      No other schemas contain the field 'Product.id'.

            Unable to access the field 'Product.price' on path 'A:Query.products<Product>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.price<Int>'.
                Unable to satisfy the requirement '{ id }' for lookup 'productById' in schema 'B'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Product.id' on path 'A:Query.products<Product>'.
                      No other schemas contain the field 'Product.id'.
            """);
    }

    [Fact]
    public void Validate_Should_Succeed_When_OverriddenFieldIsRequiredByThirdSchema()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    products: [Product]
                    productById(id: ID!): Product @lookup @internal
                }

                type Product {
                    id: ID! @shareable
                    weight: Int
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    productById(id: ID!): Product @lookup @internal
                }

                type Product {
                    id: ID! @shareable
                    weight: Int @override(from: "A")
                }
                """),
            new SourceSchemaText(
                "C",
                """
                type Query {
                    productById(id: ID!): Product @lookup @internal
                }

                type Product {
                    id: ID! @shareable
                    shippingCost(weight: Int @require(field: "weight")): Int
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Validate_Should_Succeed_When_OverriddenFieldIsLookupKeyOfOriginalSchema()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    productBySku(sku: String!): Product @lookup
                }

                type Product {
                    id: ID! @shareable
                    sku: String!
                    name: String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    products: [Product]
                    productById(id: ID!): Product @lookup @internal
                }

                type Product {
                    id: ID! @shareable
                    sku: String! @override(from: "A")
                    price: Int
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Validate_Should_Succeed_When_OverrideMovesFieldToSchemaWithLookup()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    products: [Product]
                }

                type Product {
                    id: ID! @shareable
                    name: String
                    price: Int
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    productById(id: ID!): Product @lookup @internal
                }

                type Product {
                    id: ID! @shareable
                    price: Int @override(from: "A")
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Validate_Should_Fail_When_OverrideMovesFieldToSchemaWithoutLookup()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    products: [Product]
                    productById(id: ID!): Product @lookup @internal
                }

                type Product {
                    id: ID! @shareable
                    name: String
                    price: Int
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    discountedProducts: [Product]
                }

                type Product {
                    id: ID! @shareable
                    price: Int @override(from: "A")
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.price' on path 'A:Query.products<Product>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.price<Int>'.
                No lookups found for type 'Product' in schema 'B'.
            """);
    }
}

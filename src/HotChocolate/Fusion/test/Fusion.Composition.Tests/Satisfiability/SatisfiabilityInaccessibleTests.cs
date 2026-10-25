using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityInaccessibleTests
{
    [Fact]
    public void Validate_Should_Succeed_When_UnreachableFieldIsInaccessible()
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
                    warehouseProducts: [Product]
                }

                type Product {
                    id: ID! @shareable
                    binLocation: String @inaccessible
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
    public void Validate_Should_Fail_When_FieldIsOnlyReachableThroughInaccessibleRootField()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    featuredProduct: Product
                }

                type Product {
                    sku: String! @shareable
                    name: String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    inventoryProduct: Product @inaccessible
                }

                type Product {
                    sku: String! @shareable
                    stock: Int
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
            Unable to access the field 'Product.stock' on path 'A:Query.featuredProduct<Product>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.stock<Int>'.
                No lookups found for type 'Product' in schema 'B'.
            """);
    }
}

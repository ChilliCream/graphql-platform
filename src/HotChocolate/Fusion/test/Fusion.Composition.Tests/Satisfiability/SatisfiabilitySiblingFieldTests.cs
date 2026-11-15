using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilitySiblingFieldTests
{
    [Theory]
    [InlineData("A", "B", true)]
    [InlineData("A", "B", false)]
    [InlineData("B", "A", true)]
    [InlineData("B", "A", false)]
    public void Validate_Should_Succeed_When_SiblingFieldsReturnSameValueTypeThroughSharedPath(
        string namesSchemaName,
        string pricesSchemaName,
        bool featuredFirst)
    {
        // arrange
        var (firstSibling, secondSibling) = featuredFirst
            ? ("featured: Product @shareable", "archived: Product @shareable")
            : ("archived: Product @shareable", "featured: Product @shareable");

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                namesSchemaName,
                $$"""
                type Query {
                    catalog: Catalog @shareable
                }

                type Catalog {
                    {{firstSibling}}
                    {{secondSibling}}
                }

                type Product {
                    sku: String @shareable
                    name: String
                }
                """),
            new SourceSchemaText(
                pricesSchemaName,
                $$"""
                type Query {
                    catalog: Catalog @shareable
                }

                type Catalog {
                    {{firstSibling}}
                    {{secondSibling}}
                }

                type Product {
                    sku: String @shareable
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

    [Theory]
    [InlineData("A", "B", true)]
    [InlineData("A", "B", false)]
    [InlineData("B", "A", true)]
    [InlineData("B", "A", false)]
    public void Validate_Should_Succeed_When_SiblingFieldsBelowEntityWithDifferentKeysReturnSameType(
        string idKeySchemaName,
        string codeKeySchemaName,
        bool featuredFirst)
    {
        // arrange
        var (firstSibling, secondSibling) = featuredFirst
            ? ("featured: Product @shareable", "archived: Product @shareable")
            : ("archived: Product @shareable", "featured: Product @shareable");

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                idKeySchemaName,
                $$"""
                type Query {
                    storeById(id: ID!): Store @lookup
                }

                type Store {
                    id: ID! @shareable
                    code: String! @shareable
                    {{firstSibling}}
                    {{secondSibling}}
                }

                type Product {
                    name: String
                }
                """),
            new SourceSchemaText(
                codeKeySchemaName,
                $$"""
                type Query {
                    storeByCode(code: String!): Store @lookup
                }

                type Store {
                    id: ID! @shareable
                    code: String! @shareable
                    {{firstSibling}}
                    {{secondSibling}}
                }

                type Product {
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
}

using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityFactsBuilderTests
{
    [Fact]
    public void FieldAccessible_Should_BeTrue_When_FieldExistsInSingleSchema()
    {
        // arrange
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product {
                id: ID!
                name: String
            }
            """
        ]);
        var product = GetObjectType(schema, "Product");
        var name = product.Fields["name"];

        // act
        var facts = BuildFacts(schema);

        // assert
        Assert.True(facts.IsFieldAccessible(product, name, "A"));
    }

    [Fact]
    public void CanTransitionAndFieldAccessible_Should_BeTrue_When_TargetHasLookupAndKeyIsAccessible()
    {
        // arrange
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product {
                id: ID! @shareable
                name: String
            }
            """,
            """
            # Schema B
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product {
                id: ID! @shareable
                price: Int
            }
            """
        ]);
        var product = GetObjectType(schema, "Product");
        var price = product.Fields["price"];

        // act
        var facts = BuildFacts(schema);

        // assert
        Assert.True(facts.CanTransition(product, "B", "A"));
        Assert.True(facts.IsFieldAccessible(product, price, "A"));
    }

    [Fact]
    public void CanTransitionAndFieldAccessible_Should_BeFalse_When_TargetHasNoLookup()
    {
        // arrange
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product {
                id: ID! @shareable
                name: String
            }
            """,
            """
            # Schema B
            type Query {
                product: Product
            }

            type Product {
                id: ID! @shareable
                price: Int
            }
            """
        ]);
        var product = GetObjectType(schema, "Product");
        var price = product.Fields["price"];

        // act
        var facts = BuildFacts(schema);

        // assert
        Assert.False(facts.CanTransition(product, "B", "A"));
        Assert.False(facts.IsFieldAccessible(product, price, "A"));
    }

    [Fact]
    public void FieldAccessible_Should_BeFalse_When_RequiresAreCircular()
    {
        // arrange
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product {
                id: ID! @shareable
                sku(description: String @require(field: "description")): String
            }
            """,
            """
            # Schema B
            type Query {
                productInBById(id: ID!): Product @lookup
            }

            type Product {
                id: ID! @shareable
                description(sku: String @require(field: "sku")): String
            }
            """
        ]);
        var product = GetObjectType(schema, "Product");
        var sku = product.Fields["sku"];
        var description = product.Fields["description"];

        // act
        var facts = BuildFacts(schema);

        // assert
        Assert.False(facts.IsFieldAccessible(product, sku, "A"));
        Assert.False(facts.IsFieldAccessible(product, description, "B"));
    }

    [Fact]
    public void FieldAccessible_Should_BeTrue_When_RequireCycleHasPlainSourceEscape()
    {
        // arrange
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product {
                id: ID! @shareable
                sku(description: String @require(field: "description")): String
            }
            """,
            """
            # Schema B
            type Query {
                productInBById(id: ID!): Product @lookup
            }

            type Product {
                id: ID! @shareable
                description(sku: String @require(field: "sku")): String @shareable
            }
            """,
            """
            # Schema C
            type Query {
                productInCById(id: ID!): Product @lookup
            }

            type Product {
                id: ID! @shareable
                description: String @shareable
            }
            """
        ]);
        var product = GetObjectType(schema, "Product");
        var sku = product.Fields["sku"];
        var description = product.Fields["description"];

        // act
        var facts = BuildFacts(schema);

        // assert
        Assert.True(facts.IsFieldAccessible(product, description, "A"));
        Assert.True(facts.IsFieldAccessible(product, description, "B"));
        Assert.True(facts.IsFieldAccessible(product, description, "C"));
        Assert.True(facts.IsFieldAccessible(product, sku, "A"));
    }

    private static SatisfiabilityFacts BuildFacts(MutableSchemaDefinition schema)
        => new SatisfiabilityFactsBuilder(schema, new FusionLookupDirectiveCache(schema)).Build();

    private static MutableSchemaDefinition CreateMergedSchema(string[] schemas)
    {
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(schemas),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        return merger.Merge().Value;
    }

    private static MutableObjectTypeDefinition GetObjectType(
        MutableSchemaDefinition schema,
        string typeName)
        => (MutableObjectTypeDefinition)schema.Types[typeName];
}

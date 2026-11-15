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

    [Fact]
    public void FieldResolvable_Should_BeFalse_When_RequiringSchemaHasNoLookupForType()
    {
        // arrange
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                cart: Cart
            }

            type Cart {
                items: [CartItem!]
            }

            type CartItem {
                id: ID! @shareable
                unitPrice(price: Float! @require(field: "product.discountedPrice")): Float!
            }
            """,
            """
            # Schema B
            type Query {
                cartItemById(id: ID!): CartItem @lookup @internal
            }

            type CartItem {
                id: ID! @shareable
                product: Product!
            }

            type Product {
                id: ID!
                discountedPrice: Float!
            }
            """
        ]);
        var cartItem = GetObjectType(schema, "CartItem");
        var unitPrice = cartItem.Fields["unitPrice"];

        // act
        var facts = BuildFacts(schema);

        // assert
        Assert.False(facts.IsFieldResolvableOn(cartItem, unitPrice, "A"));
    }

    [Fact]
    public void FieldResolvable_Should_BeTrue_When_RequiringSchemaHasLookupForType()
    {
        // arrange
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                cart: Cart
                cartItemById(id: ID!): CartItem @lookup @internal
            }

            type Cart {
                items: [CartItem!]
            }

            type CartItem {
                id: ID! @shareable
                unitPrice(price: Float! @require(field: "product.discountedPrice")): Float!
            }
            """,
            """
            # Schema B
            type Query {
                cartItemById(id: ID!): CartItem @lookup @internal
            }

            type CartItem {
                id: ID! @shareable
                product: Product!
            }

            type Product {
                id: ID!
                discountedPrice: Float!
            }
            """
        ]);
        var cartItem = GetObjectType(schema, "CartItem");
        var unitPrice = cartItem.Fields["unitPrice"];

        // act
        var facts = BuildFacts(schema);

        // assert
        Assert.True(facts.IsFieldResolvableOn(cartItem, unitPrice, "A"));
    }

    [Fact]
    public void FieldResolvable_Should_BeTrue_When_NestedRequiredFieldReentersRequiringSchema()
    {
        // arrange
        // the facts only transition through lookups, so B declares an Order lookup
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                orderById(id: ID!): Order @lookup @internal
                customerById(id: ID!): Customer @lookup @internal
            }

            type Order {
                id: ID! @shareable
                price(discount: Int @require(field: "customer.discount")): Int
            }

            type Customer {
                id: ID! @shareable
                discount: Int
            }
            """,
            """
            # Schema B
            type Query {
                orders: [Order]
                orderById(id: ID!): Order @lookup @internal
            }

            type Order {
                id: ID! @shareable
                customer: Customer
            }

            type Customer {
                id: ID! @shareable
            }
            """
        ]);
        var order = GetObjectType(schema, "Order");
        var price = order.Fields["price"];

        // act
        var facts = BuildFacts(schema);

        // assert
        Assert.True(facts.IsFieldResolvableOn(order, price, "A"));
    }

    [Fact]
    public void FieldResolvable_Should_BeFalse_When_FirstLevelRequiredFieldIsOnlyInRequiringSchema()
    {
        // arrange
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                orders: [Order]
                orderById(id: ID!): Order @lookup @internal
            }

            type Order {
                id: ID! @shareable
                customer: Customer
                price(discount: Int @require(field: "customer.discount")): Int
            }

            type Customer {
                id: ID! @shareable
            }
            """,
            """
            # Schema B
            type Query {
                customerById(id: ID!): Customer @lookup @internal
            }

            type Customer {
                id: ID! @shareable
                discount: Int
            }
            """
        ]);
        var order = GetObjectType(schema, "Order");
        var price = order.Fields["price"];

        // act
        var facts = BuildFacts(schema);

        // assert
        Assert.False(facts.IsFieldResolvableOn(order, price, "A"));
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

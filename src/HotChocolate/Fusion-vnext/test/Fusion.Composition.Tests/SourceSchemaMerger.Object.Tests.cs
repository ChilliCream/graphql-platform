using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerObjectTests
{
    [Theory]
    [MemberData(nameof(ExamplesData))]
    public void Examples(string[] sdl, string executionSchema)
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(sdl),
            new SourceSchemaMergerOptions
            {
                RemoveUnreferencedTypes = false,
                AddFusionDefinitions = false
            });

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchInlineSnapshot(executionSchema);
    }

    public static TheoryData<string[], string> ExamplesData()
    {
        return new TheoryData<string[], string>
        {
            // In this example, the "Product" type from two schemas is merged. The "id" field is
            // shared across both schemas, while "name" and "price" fields are contributed by the
            // individual source schemas. The resulting composed type includes all fields.
            {
                [
                    """
                    # Schema A
                    type Product @key(fields: "id") {
                        id: ID!
                        name: String
                    }
                    """,
                    """
                    # Schema B
                    type Product @key(fields: "id") {
                        id: ID!
                        price: Int
                    }
                    """
                ],
                """
                type Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                    name: String
                        @fusion__field(schema: A)
                    price: Int
                        @fusion__field(schema: B)
                }
                """
            },
            // Another example demonstrates preserving descriptions during merging. In this case,
            // the description from the first schema is retained, while the fields are merged from
            // both schemas to create the final "Order" type.
            {
                [
                    """"
                    # Schema A
                    """
                    First Description
                    """
                    type Order @key(fields: "id") {
                        id: ID!
                    }
                    """",
                    """"
                    # Schema B
                    """
                    Second Description
                    """
                    type Order @key(fields: "id") {
                        id: ID!
                        total: Float
                    }
                    """"
                ],
                """
                "First Description"
                type Order
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                    total: Float
                        @fusion__field(schema: B)
                }
                """
            },
            // In the following example, one of the "Product" types is marked with @internal. All
            // its fields are excluded from the composed type.
            {
                [
                    """
                    # Schema A
                    type Product @key(fields: "id") {
                        id: ID!
                        name: String
                    }
                    """,
                    """
                    # Schema B
                    type Product @key(fields: "id") @internal {
                        id: ID!
                        price: Int
                    }
                    """
                ],
                """
                type Product
                    @fusion__type(schema: A) {
                    id: ID!
                        @fusion__field(schema: A)
                    name: String
                        @fusion__field(schema: A)
                }
                """
            },
            // If any of the types is marked as @inaccessible, then the merged type is also marked
            // as @inaccessible in the execution schema.
            {
                [
                    """
                    # Schema A
                    type Product {
                        id: ID!
                    }
                    """,
                    """
                    # Schema B
                    type Product @inaccessible {
                        id: ID!
                    }
                    """
                ],
                """
                type Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__inaccessible {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }
                """
            },
            // Implemented interfaces. I2 is inaccessible.
            {
                [
                    """
                    # Schema A
                    interface I1 {
                        id: ID!
                    }

                    interface I2 {
                        id: ID!
                    }

                    type Product implements I1 & I2 {
                        id: ID!
                    }
                    """,
                    """
                    interface I1 {
                        id: ID!
                    }

                    interface I2 @inaccessible {
                        id: ID!
                    }

                    interface I3 {
                        id: ID!
                    }

                    type Product implements I1 & I2 & I3 {
                        id: ID!
                    }
                    """
                ],
                """
                type Product implements I1 & I3
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__implements(schema: A, interface: "I1")
                    @fusion__implements(schema: B, interface: "I1")
                    @fusion__implements(schema: B, interface: "I3") {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }

                interface I1
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }

                interface I2
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__inaccessible {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }

                interface I3
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__field(schema: B)
                }
                """
            },
            // @lookup
            {
                [
                    """
                    # Schema A
                    type Query {
                        version: Int # NOT a lookup field.
                        productById(id: ID!): Product @lookup
                        productByName(name: String!): Product @lookup
                    }

                    type Product @key(fields: "id") @key(fields: "name") {
                        id: ID!
                        name: String!
                    }
                    """
                ],
                """
                schema {
                    query: Query
                }

                type Query
                    @fusion__type(schema: A) {
                    productById(id: ID!
                        @fusion__inputField(schema: A)): Product
                        @fusion__field(schema: A)
                    productByName(name: String!
                        @fusion__inputField(schema: A)): Product
                        @fusion__field(schema: A)
                    version: Int
                        @fusion__field(schema: A)
                }

                type Product
                    @fusion__type(schema: A)
                    @fusion__lookup(schema: A, key: "id", field: "productById(id: ID!): Product", map: [ "id" ], path: null, internal: false)
                    @fusion__lookup(schema: A, key: "name", field: "productByName(name: String!): Product", map: [ "name" ], path: null, internal: false) {
                    id: ID!
                        @fusion__field(schema: A)
                    name: String!
                        @fusion__field(schema: A)
                }
                """
            },
            // Nested @lookup
            {
                [
                    """
                    # Schema A
                    type Query {
                        productById(id: ID!): Product @lookup
                        productBySku(sku: String!): Product @lookup
                    }

                    type Product @key(fields: "id") {
                        id: ID!
                        price(regionName: String!): ProductPrice @lookup
                    }

                    type ProductPrice @key(fields: "regionName product { id }") {
                        regionName: String!
                        product: Product
                        value: Float!
                    }
                    """
                ],
                """
                schema {
                    query: Query
                }

                type Query
                    @fusion__type(schema: A) {
                    productById(id: ID!
                        @fusion__inputField(schema: A)): Product
                        @fusion__field(schema: A)
                    productBySku(sku: String!
                        @fusion__inputField(schema: A)): Product
                        @fusion__field(schema: A)
                }

                type Product
                    @fusion__type(schema: A)
                    @fusion__lookup(schema: A, key: "id", field: "productById(id: ID!): Product", map: [ "id" ], path: null, internal: false)
                    @fusion__lookup(schema: A, key: "sku", field: "productBySku(sku: String!): Product", map: [ "sku" ], path: null, internal: false) {
                    id: ID!
                        @fusion__field(schema: A)
                    price(regionName: String!
                        @fusion__inputField(schema: A)): ProductPrice
                        @fusion__field(schema: A)
                }

                type ProductPrice
                    @fusion__type(schema: A)
                    @fusion__lookup(schema: A, key: "regionName", field: "price(regionName: String!): ProductPrice", map: [ "regionName" ], path: "productById", internal: false)
                    @fusion__lookup(schema: A, key: "regionName", field: "price(regionName: String!): ProductPrice", map: [ "regionName" ], path: "productBySku", internal: false) {
                    product: Product
                        @fusion__field(schema: A)
                    regionName: String!
                        @fusion__field(schema: A)
                    value: Float!
                        @fusion__field(schema: A)
                }
                """
            },
            // Deeply-nested @lookup
            {
                [
                    """
                    # Schema A
                    type Query {
                        productById(id: ID!): Product @lookup
                        lookups1: Lookups1!
                    }

                    type Lookups1 {
                        productBySku(sku: String!): Product @lookup
                        lookups2: Lookups2!
                    }

                    type Lookups2 {
                        productByName(name: String!): Product @lookup
                    }

                    type Product @key(fields: "id") {
                        id: ID!
                    }
                    """
                ],
                """
                schema {
                    query: Query
                }

                type Query
                    @fusion__type(schema: A) {
                    lookups1: Lookups1!
                        @fusion__field(schema: A)
                    productById(id: ID!
                        @fusion__inputField(schema: A)): Product
                        @fusion__field(schema: A)
                }

                type Lookups1
                    @fusion__type(schema: A) {
                    lookups2: Lookups2!
                        @fusion__field(schema: A)
                    productBySku(sku: String!
                        @fusion__inputField(schema: A)): Product
                        @fusion__field(schema: A)
                }

                type Lookups2
                    @fusion__type(schema: A) {
                    productByName(name: String!
                        @fusion__inputField(schema: A)): Product
                        @fusion__field(schema: A)
                }

                type Product
                    @fusion__type(schema: A)
                    @fusion__lookup(schema: A, key: "id", field: "productById(id: ID!): Product", map: [ "id" ], path: null, internal: false)
                    @fusion__lookup(schema: A, key: "sku", field: "productBySku(sku: String!): Product", map: [ "sku" ], path: "lookups1", internal: false)
                    @fusion__lookup(schema: A, key: "name", field: "productByName(name: String!): Product", map: [ "name" ], path: "lookups1.lookups2", internal: false) {
                    id: ID!
                        @fusion__field(schema: A)
                }
                """
            },
            // @lookup with @is
            {
                [
                    """
                    # Schema A
                    type Query {
                        personByAddressId(id: ID! @is(field: "address.id")): Person @lookup
                    }

                    type Person @key(fields: "address { id }") {
                        id: ID!
                        address: Address
                    }

                    type Address {
                        id: ID!
                    }
                    """
                ],
                """
                schema {
                    query: Query
                }

                type Query
                    @fusion__type(schema: A) {
                    personByAddressId(id: ID!
                        @fusion__inputField(schema: A)): Person
                        @fusion__field(schema: A)
                }

                type Address
                    @fusion__type(schema: A) {
                    id: ID!
                        @fusion__field(schema: A)
                }

                type Person
                    @fusion__type(schema: A)
                    @fusion__lookup(schema: A, key: "address { id }", field: "personByAddressId(id: ID!): Person", map: [ "address.id" ], path: null, internal: false) {
                    address: Address
                        @fusion__field(schema: A)
                    id: ID!
                        @fusion__field(schema: A)
                }
                """
            },
            // @lookup in multiple schemas.
            {
                [
                    """
                    # Schema A
                    type Query {
                        personById(id: ID!): Person @lookup
                    }

                    type Person @key(fields: "id") {
                        id: ID!
                    }
                    """,
                    """
                    # Schema B
                    type Query {
                        personBySku(sku: String!): Person @lookup
                    }

                    type Person @key(fields: "sku") {
                        sku: String!
                    }
                    """
                ],
                """
                schema {
                    query: Query
                }

                type Query
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    personById(id: ID!
                        @fusion__inputField(schema: A)): Person
                        @fusion__field(schema: A)
                    personBySku(sku: String!
                        @fusion__inputField(schema: B)): Person
                        @fusion__field(schema: B)
                }

                type Person
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__lookup(schema: A, key: "id", field: "personById(id: ID!): Person", map: [ "id" ], path: null, internal: false)
                    @fusion__lookup(schema: B, key: "sku", field: "personBySku(sku: String!): Person", map: [ "sku" ], path: null, internal: false) {
                    id: ID!
                        @fusion__field(schema: A)
                    sku: String!
                        @fusion__field(schema: B)
                }
                """
            },
            // @lookup on field with multiple arguments.
            {
                [
                    """
                    type Query {
                        productByIdAndCategoryId(id: ID!, categoryId: Int): Product! @lookup
                    }

                    type Product {
                        id: ID!
                        categoryId: Int
                    }
                    """
                ],
                """
                schema {
                    query: Query
                }

                type Query
                    @fusion__type(schema: A) {
                    productByIdAndCategoryId(categoryId: Int
                        @fusion__inputField(schema: A) id: ID!
                        @fusion__inputField(schema: A)): Product!
                        @fusion__field(schema: A)
                }

                type Product
                    @fusion__type(schema: A)
                    @fusion__lookup(schema: A, key: "id categoryId", field: "productByIdAndCategoryId(id: ID! categoryId: Int): Product!", map: [ "id", "categoryId" ], path: null, internal: false) {
                    categoryId: Int
                        @fusion__field(schema: A)
                    id: ID!
                        @fusion__field(schema: A)
                }
                """
            },
            // Internal lookup.
            {
                [
                    """
                    type Query {
                        product: Product
                        productById(id: ID!): Product @lookup @internal
                    }

                    type Product @key(fields: "id") {
                        id: ID!
                    }
                    """
                ],
                """
                schema {
                    query: Query
                }

                type Query
                    @fusion__type(schema: A) {
                    product: Product
                        @fusion__field(schema: A)
                }

                type Product
                    @fusion__type(schema: A)
                    @fusion__lookup(schema: A, key: "id", field: "productById(id: ID!): Product", map: [ "id" ], path: null, internal: true) {
                    id: ID!
                        @fusion__field(schema: A)
                }
                """
            }
        };
    }
}

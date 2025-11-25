namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerObjectTests : SourceSchemaMergerTestBase
{
    // In this example, the "Product" type from two schemas is merged. The "id" field is shared
    // across both schemas, while "name" and "price" fields are contributed by the individual source
    // schemas. The resulting composed type includes all fields.
    [Fact]
    public void Merge_Objects_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // Another example demonstrates preserving descriptions during merging. In this case, the
    // description from the first schema is retained, while the fields are merged from both schemas
    // to create the final "Order" type.
    [Fact]
    public void Merge_ObjectsUsesFirstNonNullDescription_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // In the following example, one of the "Product" types is marked with @internal. All its fields
    // are excluded from the composed type.
    [Fact]
    public void Merge_InternalObject_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // If any of the types is marked as @inaccessible, then the merged type is also marked as
    // @inaccessible in the execution schema.
    [Fact]
    public void Merge_InaccessibleObject_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // Implemented interfaces. I2 is inaccessible.
    [Fact]
    public void Merge_InaccessibleImplementedInterface_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // @lookup
    [Fact]
    public void Merge_ObjectWithLookup_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // Nested @lookup
    [Fact]
    public void Merge_ObjectWithNestedLookup_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // Deeply-nested @lookup
    [Fact]
    public void Merge_ObjectWithDeeplyNestedLookup_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // @lookup with @is
    [Fact]
    public void Merge_ObjectWithLookupUsingIsDirective_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // @lookup in multiple schemas.
    [Fact]
    public void Merge_ObjectsWithLookup_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // @lookup on field with multiple arguments.
    [Fact]
    public void Merge_ObjectWithLookupMultipleArguments_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // @oneOf lookups are split.
    [Fact]
    public void Merge_ObjectWithOneOfLookups_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                type Query {
                    brand1(
                        by: BrandByInput1! @is(field: "{ id } | { key }")
                    ): Brand @lookup
                    brand2(
                        name: String!
                        and: BrandByInput1! @is(field: "{ id } | { key }")
                    ): Brand @lookup
                    brand3(
                        by: BrandByInput1! @is(field: "{ id } | { key }")
                        and: BrandByInput2! @is(field: "{ name } | { title }")
                    ): Brand @lookup
                }

                type Brand @key(fields: "id") {
                    id: Int!
                    key: String!
                    name: String!
                    title: String
                }

                input BrandByInput1 @oneOf {
                    id: Int
                    key: String
                }

                input BrandByInput2 @oneOf {
                    name: String
                    title: String
                }
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A) {
                brand1(by: BrandByInput1!
                    @fusion__inputField(schema: A)): Brand
                    @fusion__field(schema: A)
                brand2(and: BrandByInput1!
                    @fusion__inputField(schema: A) name: String!
                    @fusion__inputField(schema: A)): Brand
                    @fusion__field(schema: A)
                brand3(and: BrandByInput2!
                    @fusion__inputField(schema: A) by: BrandByInput1!
                    @fusion__inputField(schema: A)): Brand
                    @fusion__field(schema: A)
            }

            type Brand
                @fusion__type(schema: A)
                @fusion__lookup(schema: A, key: "id", field: "brand1(by: BrandByInput1!): Brand", map: [ "{ id }" ], path: null, internal: false)
                @fusion__lookup(schema: A, key: "key", field: "brand1(by: BrandByInput1!): Brand", map: [ "{ key }" ], path: null, internal: false)
                @fusion__lookup(schema: A, key: "name id", field: "brand2(name: String! and: BrandByInput1!): Brand", map: [ "name", "{ id }" ], path: null, internal: false)
                @fusion__lookup(schema: A, key: "name key", field: "brand2(name: String! and: BrandByInput1!): Brand", map: [ "name", "{ key }" ], path: null, internal: false)
                @fusion__lookup(schema: A, key: "id name", field: "brand3(by: BrandByInput1! and: BrandByInput2!): Brand", map: [ "{ id }", "{ name }" ], path: null, internal: false)
                @fusion__lookup(schema: A, key: "key name", field: "brand3(by: BrandByInput1! and: BrandByInput2!): Brand", map: [ "{ key }", "{ name }" ], path: null, internal: false)
                @fusion__lookup(schema: A, key: "id title", field: "brand3(by: BrandByInput1! and: BrandByInput2!): Brand", map: [ "{ id }", "{ title }" ], path: null, internal: false)
                @fusion__lookup(schema: A, key: "key title", field: "brand3(by: BrandByInput1! and: BrandByInput2!): Brand", map: [ "{ key }", "{ title }" ], path: null, internal: false) {
                id: Int!
                    @fusion__field(schema: A)
                key: String!
                    @fusion__field(schema: A)
                name: String!
                    @fusion__field(schema: A)
                title: String
                    @fusion__field(schema: A)
            }

            input BrandByInput1
                @oneOf
                @fusion__type(schema: A) {
                id: Int
                    @fusion__inputField(schema: A)
                key: String
                    @fusion__inputField(schema: A)
            }

            input BrandByInput2
                @oneOf
                @fusion__type(schema: A) {
                name: String
                    @fusion__inputField(schema: A)
                title: String
                    @fusion__inputField(schema: A)
            }
            """);
    }

    // Internal lookup.
    [Fact]
    public void Merge_ObjectWithInternalLookup_MatchesSnapshot()
    {
        AssertMatches(
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
            """);
    }

    // Lookup field and argument have a description.
    [Fact]
    public void Merge_ObjectWithLookupIncludingDescriptions_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                type Query {
                    "Fetches a product"
                    productById("The product id" id: ID!): Product @lookup
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
                "Fetches a product"
                productById("The product id" id: ID!
                    @fusion__inputField(schema: A)): Product
                    @fusion__field(schema: A)
            }

            type Product
                @fusion__type(schema: A)
                @fusion__lookup(schema: A, key: "id", field: "productById(id: ID!): Product", map: [ "id" ], path: null, internal: false) {
                id: ID!
                    @fusion__field(schema: A)
            }
            """);
    }
}

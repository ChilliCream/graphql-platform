using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion;

public sealed class SatisfiabilityValidatorTests
{
    // Tests from the specification.
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-Validate-Satisfiability

    [Fact]
    public void ExtendingATypeWithNoLookup()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Profile Schema
                type Query {
                    profileById(id: ID!): Profile
                }

                type Profile {
                    id: ID!
                    user: User
                }

                type User {
                    id: ID!
                    name: String
                }
                """,
                """
                # Order Schema
                type Query {
                    orders: [Order]
                }

                type Order {
                    id: ID!
                    user: User
                }

                type User {
                    id: ID!
                    membershipStatus: String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'User.membershipStatus' on path 'A:Query.profileById<Profile> -> A:Profile.user<User>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:User.membershipStatus<String>'.
                No lookups found for type 'User' in schema 'B'.

            Unable to access the field 'User.name' on path 'B:Query.orders<Order> -> B:Order.user<User>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:User.name<String>'.
                No lookups found for type 'User' in schema 'A'.
            """);
    }

    [Fact]
    public void CompositeKeyFieldMissingInTheSecondSchema()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema Product
                type Query {
                    # A composite lookup that uses both `id` and `sku`
                    productByIdSku(id: ID!, sku: String!): Product @lookup
                }

                type Product {
                    id: ID!
                    sku: String!
                    name: String
                }
                """,
                """
                # Schema Inventory
                type Query {
                    # A lookup that uses only `id`
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    stock: Int
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.sku' on path 'B:Query.productById<Product>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.sku<String>'.
                Unable to satisfy the requirement '{ id sku }' for lookup 'A:Query.productByIdSku<Product>'.
                  Unable to satisfy the requirement 'sku'.
                    Unable to access the required field 'Product.sku' on path 'B:Query.productById<Product>'.
                        No other schemas contain the field 'Product.sku'.

            Unable to access the field 'Product.name' on path 'B:Query.productById<Product>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.name<String>'.
                Unable to satisfy the requirement '{ id sku }' for lookup 'A:Query.productByIdSku<Product>'.
                  Unable to satisfy the requirement 'sku'.
                    Unable to access the required field 'Product.sku' on path 'B:Query.productById<Product>'.
                        No other schemas contain the field 'Product.sku'.
            """);
    }

    [Fact]
    public void KeyMismatchBetweenSourceSchemas()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema Products
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    description: String
                }
                """,
                """
                # Schema Inventory
                type Query {
                    productBySku(sku: String!): Product @lookup
                }

                type Product {
                    sku: String!
                    stock: Int!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.sku' on path 'A:Query.productById<Product>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.sku<String>'.
                Unable to satisfy the requirement '{ sku }' for lookup 'B:Query.productBySku<Product>'.
                  Unable to satisfy the requirement 'sku'.
                    Unable to access the required field 'Product.sku' on path 'A:Query.productById<Product>'.
                      No other schemas contain the field 'Product.sku'.

            Unable to access the field 'Product.stock' on path 'A:Query.productById<Product>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.stock<Int>'.
                Unable to satisfy the requirement '{ sku }' for lookup 'B:Query.productBySku<Product>'.
                  Unable to satisfy the requirement 'sku'.
                    Unable to access the required field 'Product.sku' on path 'A:Query.productById<Product>'.
                      No other schemas contain the field 'Product.sku'.

            Unable to access the field 'Product.id' on path 'B:Query.productBySku<Product>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.id<ID>'.
                Unable to satisfy the requirement '{ id }' for lookup 'A:Query.productById<Product>'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Product.id' on path 'B:Query.productBySku<Product>'.
                      No other schemas contain the field 'Product.id'.

            Unable to access the field 'Product.description' on path 'B:Query.productBySku<Product>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.description<String>'.
                Unable to satisfy the requirement '{ id }' for lookup 'A:Query.productById<Product>'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Product.id' on path 'B:Query.productBySku<Product>'.
                      No other schemas contain the field 'Product.id'.
            """);
    }

    [Fact]
    public void KeyMismatchBetweenSourceSchemas_Fixed()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema Products
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    description: String
                }
                """,
                """
                # Schema Inventory
                type Query {
                    productBySku(sku: String!): Product @lookup
                }

                type Product {
                    sku: String!
                    stock: Int!
                }
                """,
                """
                # Schema ProductIndex
                type Query {
                    productById(id: ID!): Product @lookup
                    productBySku(sku: String!): Product @lookup
                }

                type Product {
                    id: ID!
                    sku: String!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void MissingFieldInASharedValueType()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema User
                type Query {
                    userById(id: ID!): User @lookup
                }

                type User {
                    id: ID!
                    address: Address
                }

                type Address {
                    street: String
                    city: String
                }
                """,
                """
                # Schema Orders
                type Query {
                    orderById(id: ID!): Order @lookup
                }

                type Order {
                    id: ID!
                    shippingAddress: Address
                }

                type Address {
                    street: String
                    city: String
                    country: String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Address.country' on path 'A:Query.userById<User> -> A:User.address<Address>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Address.country<String>'.
                No lookups found for type 'Address' in schema 'B'.
            """);
    }

    [Fact]
    public void EntityVsValueTypeConflict()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema Products
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    # Category is just a nested value type here (no @lookup, no id).
                    category: Category
                }

                type Category {
                    name: String
                }
                """,
                """
                # Schema Categories
                type Query {
                    # Category is treated as an entity with its own lookup
                    categoryById(id: ID!): Category @lookup
                }

                type Category {
                    id: ID!
                    description: String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Category.id' on path 'A:Query.productById<Product> -> A:Product.category<Category>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Category.id<ID>'.
                Unable to satisfy the requirement '{ id }' for lookup 'B:Query.categoryById<Category>'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Category.id' on path 'A:Product.category<Category>'.
                        No other schemas contain the field 'Category.id'.

            Unable to access the field 'Category.description' on path 'A:Query.productById<Product> -> A:Product.category<Category>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Category.description<String>'.
                Unable to satisfy the requirement '{ id }' for lookup 'B:Query.categoryById<Category>'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Category.id' on path 'A:Product.category<Category>'.
                        No other schemas contain the field 'Category.id'.

            Unable to access the field 'Category.name' on path 'B:Query.categoryById<Category>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Category.name<String>'.
                No lookups found for type 'Category' in schema 'A'.
            """);
    }

    [Fact]
    public void SharedEntityWithoutALookup()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema Order
                type Query {
                    orderById(id: ID!): Order @lookup
                }

                type Order {
                    id: ID! @shareable
                    user: User
                }

                type User @key(fields: "id") {
                    id: ID!
                    name: String
                }
                """,
                """
                # Schema User
                type Query {
                    userById(id: ID!): User @lookup
                }

                type User @key(fields: "id") {
                    id: ID!
                    name: String
                    email: String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    // Tests derived from the federation-gateway-audit project.
    // https://github.com/graphql-hive/federation-gateway-audit/tree/main/src/test-suites

    [Fact]
    //
    public void AbstractTypes()
    {
        // todo use abstract lookup instead for PublisherType in schema A?
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    agencyById(id: ID!): Agency @lookup @inaccessible # Added
                    groupById(id: ID!): Group @lookup @inaccessible # Added
                }

                type Agency @key(fields: "id") {
                    id: ID!
                    companyName: String
                }

                type Group @key(fields: "id") {
                    id: ID!
                    name: String
                }

                extend union PublisherType = Agency | Group
                """,
                """
                # Schema B
                type Book @key(fields: "id") {
                    id: ID!
                    title: String
                }

                type Query {
                    books: [Book]
                    bookById(id: ID!): Book @lookup @inaccessible # Added
                }
                """,
                """
                # Schema C
                type Query {
                    bookById(id: ID!): Book @lookup @inaccessible # Added
                    magazineById(id: ID!): Magazine @lookup @inaccessible # Added
                }

                interface Product {
                    id: ID!
                    dimensions: ProductDimension
                    delivery(zip: String): DeliveryEstimates
                }

                type Book implements Product @key(fields: "id") {
                    id: ID!
                    dimensions: ProductDimension @external
                    delivery(
                        zip: String
                        size: String @require(field: "dimensions.size")
                        weight: Float @require(field: "dimensions.weight")
                    ): DeliveryEstimates
                }

                type Magazine implements Product @key(fields: "id") {
                    id: ID!
                    dimensions: ProductDimension @external
                    delivery(
                        zip: String
                        size: String @require(field: "dimensions.size")
                        weight: Float @require(field: "dimensions.weight")
                    ): DeliveryEstimates
                }

                type ProductDimension @shareable {
                    size: String
                    weight: Float
                }

                type DeliveryEstimates {
                    estimatedDelivery: String
                    fastestDelivery: String
                }
                """,
                """
                # Schema D
                type Magazine @key(fields: "id") {
                    id: ID!
                    title: String
                }

                type Query {
                    magazines: [Magazine]
                    magazineById(id: ID!): Magazine @lookup @inaccessible # Added
                }
                """,
                """
                # Schema E
                type Query {
                    products: [Product]
                    similar(id: ID!): [Product]
                    bookById(id: ID!): Book @lookup @inaccessible # Added
                    magazineById(id: ID!): Magazine @lookup @inaccessible # Added
                }

                interface Product {
                    id: ID!
                    sku: String
                    dimensions: ProductDimension
                    createdBy: User
                    hidden: Boolean @inaccessible
                }

                interface Similar {
                    similar: [Product]
                }

                type ProductDimension @shareable {
                    size: String
                    weight: Float
                }

                type Book implements Product & Similar @key(fields: "id") {
                    id: ID!
                    sku: String
                    dimensions: ProductDimension @shareable
                    createdBy: User
                    similar: [Book]
                    hidden: Boolean
                    publisherType: PublisherType
                }

                type Magazine implements Product & Similar @key(fields: "id") {
                    id: ID!
                    sku: String
                    dimensions: ProductDimension @shareable
                    createdBy: User
                    similar: [Magazine]
                    hidden: Boolean
                    publisherType: PublisherType
                }

                union PublisherType = Agency | Self

                type Agency {
                    id: ID! @shareable
                }

                type Self {
                    email: String
                }

                type User @key(fields: "email") {
                    email: ID!
                    totalProductsCreated: Int @shareable
                }
                """,
                """
                # Schema F
                type Book implements Product & Similar @key(fields: "id") {
                    id: ID!
                    reviewsCount: Int!
                    reviewsScore: Float! @shareable
                    reviews: [Review!]!
                    similar: [Book] @external
                    reviewsOfSimilar(similarId: ID! @require(field: "similar[id]")): [Review!]!
                }

                type Magazine implements Product & Similar @key(fields: "id") {
                    id: ID!
                    reviewsCount: Int!
                    reviewsScore: Float! @shareable
                    reviews: [Review!]!
                    similar: [Magazine] @external
                    reviewsOfSimilar(similarId: ID! @require(field: "similar[id]")): [Review!]!
                }

                interface Product {
                    id: ID!
                    reviewsCount: Int!
                    reviewsScore: Float!
                    reviews: [Review!]!
                }

                interface Similar {
                    similar: [Product]
                }

                type Query {
                    review(id: Int!): Review
                    bookById(id: ID!): Book @lookup @inaccessible # Added
                    magazineById(id: ID!): Magazine @lookup @inaccessible # Added
                }

                type Review {
                    id: Int!
                    body: String!
                    product: Product
                }
                """,
                """
                # Schema G
                type Query {
                    userByEmail(email: ID!): User @lookup @inaccessible # Added
                }

                type User @key(fields: "email") {
                    email: ID!
                    name: String
                    totalProductsCreated: Int
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    // https://github.com/graphql-hive/federation-gateway-audit/tree/main/src/test-suites/circular-reference-interface
    public void CircularReferenceInterface()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    product: Product
                }

                interface Product {
                    samePriceProduct: Product
                }

                type Book implements Product @key(fields: "id") {
                    id: ID!
                    samePriceProduct: Book @provides(fields: "price")
                    price: Float @external
                }
                """,
                """
                # Schema B
                type Query {
                    bookById(id: ID!): Book @lookup @inaccessible # Added
                }

                type Book @key(fields: "id") {
                    id: ID!
                    price: Float @shareable
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    // https://github.com/graphql-hive/federation-gateway-audit/tree/main/src/test-suites/requires-interface
    public void RequiresInterface()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    a: User
                    userById(id: ID!): User @lookup @inaccessible # Added
                }

                interface Address {
                    id: ID!
                }

                type HomeAddress implements Address @key(fields: "id") {
                    id: ID!
                    city: String @shareable
                }

                type WorkAddress implements Address @key(fields: "id") {
                    id: ID!
                    city: String @shareable
                }

                type User @key(fields: "id") {
                    id: ID!
                    name: String! @shareable
                    address: Address @external
                    city(addressId: ID! @require(field: "address.id")): String
                    country(workAddressId: ID! @require(field: "address<WorkAddress>.id")): String
                }
                """,
                """
                # Schema B
                type Query {
                    b: User
                    userById(id: ID!): User @lookup @inaccessible # Added
                }

                interface Address {
                    id: ID!
                }

                type HomeAddress implements Address @key(fields: "id") {
                    id: ID!
                    city: String @shareable
                }

                type WorkAddress implements Address @key(fields: "id") {
                    id: ID!
                    city: String @shareable
                }

                type SecondAddress implements Address @key(fields: "id") {
                    id: ID!
                    city: String @shareable
                }

                type User @key(fields: "id") {
                    id: ID!
                    name: String! @shareable
                    address: Address @shareable
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    // https://github.com/graphql-hive/federation-gateway-audit/blob/main/src/test-suites/requires-requires
    public void RequiresRequires()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product @lookup @inaccessible # Added
                }

                type Product @key(fields: "id") {
                    id: ID!
                    price: Float! @inaccessible
                }
                """,
                """
                # Schema B
                type Query {
                    product: Product
                    productById(id: ID!): Product @lookup @inaccessible # Added
                }

                type Product @key(fields: "id") {
                    id: ID!
                    hasDiscount: Boolean!
                }
                """,
                """
                # Schema C
                type Query {
                    productById(id: ID!): Product @lookup @inaccessible # Added
                }

                type Product @key(fields: "id") {
                    id: ID!
                    price: Float! @external
                    isExpensive(price: Float! @require(field: "price")): Boolean!
                    hasDiscount: Boolean! @external
                    isExpensiveWithDiscount(
                        hasDiscount: Boolean! @require(field: "hasDiscount")
                    ): Boolean!
                }
                """,
                """
                # Schema D
                type Query {
                    productById(id: ID!): Product @lookup @inaccessible # Added
                }

                type Product @key(fields: "id") {
                    id: ID!
                    isExpensive: Boolean! @external
                    canAfford(isExpensive: Boolean! @require(field: "isExpensive")): Boolean!
                    isExpensiveWithDiscount: Boolean! @external
                    canAffordWithDiscount(
                        isExpensiveWithDiscount: Boolean! @require(field: "isExpensiveWithDiscount")
                    ): Boolean!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    // https://github.com/graphql-hive/federation-gateway-audit/blob/main/src/test-suites/simple-entity-call
    public void SimpleEntityCall()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    user: User
                    userById(id: ID!): User @lookup @inaccessible # Added
                }

                type User @key(fields: "id") {
                    id: ID!
                    email: String!
                }
                """,
                """
                # Schema B
                type Query {
                    userByEmail(email: String!): User @lookup @inaccessible # Added
                }

                type User @key(fields: "email") {
                    email: String! @external
                    nickname: String!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    // https://github.com/graphql-hive/federation-gateway-audit/tree/main/src/test-suites/union-interface-distributed
    public void UnionInterfaceDistributed()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    products: [Product]
                    node(id: ID!): Node
                    nodes: [Node]
                    toasters: [Toaster]
                }

                union Product = Oven | Toaster

                interface Node {
                    id: ID!
                }

                type Oven @key(fields: "id") {
                    id: ID!
                }

                type Toaster implements Node @key(fields: "id") {
                    id: ID!
                    warranty: Int
                }
                """,
                """
                # Schema B
                type Query {
                    ovenById(id: ID!): Oven @lookup @inaccessible # Added
                }

                interface Node {
                    id: ID!
                }

                type Oven implements Node @key(fields: "id") {
                    id: ID!
                    warranty: Int
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    // Other tests.

    [Fact]
    public void CycleInType()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product
                }

                type Product
                {
                    id: ID!
                    relatedProduct: Product!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CycleInRequireDirectives()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    name: String!
                    sku(description: String @require(field: "description")): String
                }
                """,
                """
                # Schema B
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    description(sku: String @require(field: "sku")): String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.sku' on path 'A:Query.productById<Product>'.
              Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                Unable to satisfy the requirement 'description'.
                  Unable to access the required field 'Product.description' on path 'A:Query.productById<Product>'.
                    Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                      Unable to satisfy the requirement 'sku'.
                        Unable to access the required field 'Product.sku' on path 'A:Query.productById<Product>'.
                          Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                            Unable to satisfy the requirement 'description'.
                              Unable to access the required field 'Product.description' on path 'A:Query.productById<Product>'.
                                Cycle detected in requirement: B:Product.description<String> -> A:Product.sku<String> -> B:Product.description<String>.

            Unable to access the field 'Product.description' on path 'A:Query.productById<Product>'.
              Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                Unable to satisfy the requirement 'sku'.
                  Unable to access the required field 'Product.sku' on path 'A:Query.productById<Product>'.
                    Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                      Unable to satisfy the requirement 'description'.
                        Unable to access the required field 'Product.description' on path 'A:Query.productById<Product>'.
                          Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                            Unable to satisfy the requirement 'sku'.
                              Unable to access the required field 'Product.sku' on path 'A:Query.productById<Product>'.
                                Cycle detected in requirement: A:Product.sku<String> -> B:Product.description<String> -> A:Product.sku<String>.

            Unable to access the field 'Product.sku' on path 'B:Query.productById<Product>'.
              Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                Unable to satisfy the requirement 'description'.
                  Unable to access the required field 'Product.description' on path 'B:Query.productById<Product>'.
                    Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                      Unable to satisfy the requirement 'sku'.
                        Unable to access the required field 'Product.sku' on path 'B:Query.productById<Product>'.
                          Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                            Unable to satisfy the requirement 'description'.
                              Unable to access the required field 'Product.description' on path 'B:Query.productById<Product>'.
                                Cycle detected in requirement: B:Product.description<String> -> A:Product.sku<String> -> B:Product.description<String>.

            Unable to access the field 'Product.description' on path 'B:Query.productById<Product>'.
              Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                Unable to satisfy the requirement 'sku'.
                  Unable to access the required field 'Product.sku' on path 'B:Query.productById<Product>'.
                    Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                      Unable to satisfy the requirement 'description'.
                        Unable to access the required field 'Product.description' on path 'B:Query.productById<Product>'.
                          Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                            Unable to satisfy the requirement 'sku'.
                              Unable to access the required field 'Product.sku' on path 'B:Query.productById<Product>'.
                                Cycle detected in requirement: A:Product.sku<String> -> B:Product.description<String> -> A:Product.sku<String>.
            """);
    }

    [Fact]
    public void InterfaceLookup()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    catById(id: ID!): Cat @lookup
                }

                type Cat {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    animalById(id: ID!): Animal @lookup
                }

                type Cat implements Animal {
                    id: ID!
                    name: String
                }

                interface Animal {
                    id: ID!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void InterfaceLookupUsingSecondLookup()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    catById(id: ID!): Cat @lookup
                }

                type Cat {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    animalById1(id: ID! @is(field: "<Dog>.id")): Animal @lookup
                    animalById2(id: ID!): Animal @lookup
                }

                type Cat implements Animal {
                    id: ID!
                    name: String
                }

                type Dog implements Animal {
                    id: ID!
                    name: String
                }

                interface Animal {
                    id: ID!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void NodeLookup()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    node(id: ID!): Node @lookup
                }

                type Product implements Node {
                    id: ID!
                    name: String
                }

                interface Node {
                    id: ID!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void NonRootLookup()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    lookups1: Lookups1!
                }

                type Lookups1 {
                    lookups2: Lookups2!
                }

                type Lookups2 {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    name: String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void SplitCompositeKey()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    products: [Product!]!
                }

                type Product {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    productById(id: ID!): Product @lookup @inaccessible
                }

                type Product {
                    id: ID!
                    keyField1: Int!
                }
                """,
                """
                # Schema C
                type Query {
                    productByKeyField1(keyField1: Int!): Product @lookup @inaccessible
                }

                type Product {
                    keyField2: Int!
                }
                """,
                """
                # Schema D
                type Query {
                    productByKey(keyField1: Int!, keyField2: Int!): Product @lookup @inaccessible
                }

                type Product {
                    keyField1: Int!
                    keyField2: Int!
                    specialField: Int!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void UnionLookup()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    catById(id: ID!): Cat @lookup
                }

                type Cat {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    animalById(id: ID!): Animal @lookup
                }

                type Cat {
                    id: ID!
                    name: String
                }

                type Dog {
                    id: ID!
                    name: String
                }

                union Animal = Cat | Dog
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void UnsatisfiableRequirements()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    title(description: String @require(field: "{ name price }")): String
                }
                """,
                """
                # Schema B
                type Product {
                    id: ID!
                    name: String
                    price: Float!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.title' on path 'A:Query.productById<Product>'.
              Unable to satisfy the requirement '{ name price }' on field 'A:Product.title<String>'.
                Unable to satisfy the requirement 'name'.
                  Unable to access the required field 'Product.name' on path 'A:Query.productById<Product>'.
                    Unable to transition between schemas 'A' and 'B' for access to required field 'B:Product.name<String>'.
                      No lookups found for type 'Product' in schema 'B'.
                Unable to satisfy the requirement 'price'.
                  Unable to access the required field 'Product.price' on path 'A:Query.productById<Product>'.
                    Unable to transition between schemas 'A' and 'B' for access to required field 'B:Product.price<Float>'.
                      No lookups found for type 'Product' in schema 'B'.

            Unable to access the field 'Product.name' on path 'A:Query.productById<Product>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.name<String>'.
                No lookups found for type 'Product' in schema 'B'.

            Unable to access the field 'Product.price' on path 'A:Query.productById<Product>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.price<Float>'.
                No lookups found for type 'Product' in schema 'B'.
            """);
    }

    [Fact]
    public void UnsatisfiableRequirementsNested()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    title(
                        input: String @require(field: "{ a: category.name b: section.name }")
                    ): String
                }
                """,
                """
                # Schema B
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    category: Category
                    section: Section
                }

                type Category {
                    id: ID!
                }

                type Section {
                    id: ID!
                }
                """,
                """
                # Schema C
                type Category {
                    id: ID!
                    name: String!
                }

                type Section {
                    id: ID!
                    name: String!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.title' on path 'A:Query.productById<Product>'.
              Unable to satisfy the requirement '{ category { name } section { name } }' on field 'A:Product.title<String>'.
                Unable to satisfy the requirement 'category { name }'.
                  Unable to access the required field 'Product.category' on path 'A:Query.productById<Product>'.
                    Unable to access the required field 'Category.name' on path 'A:Query.productById<Product> -> B:Query.productById<Product> -> B:Product.category<Category>'.
                      Unable to transition between schemas 'B' and 'C' for access to required field 'C:Category.name<String>'.
                        No lookups found for type 'Category' in schema 'C'.
                Unable to satisfy the requirement 'section { name }'.
                  Unable to access the required field 'Product.section' on path 'A:Query.productById<Product>'.
                    Unable to access the required field 'Section.name' on path 'A:Query.productById<Product> -> B:Query.productById<Product> -> B:Product.section<Section>'.
                      Unable to transition between schemas 'B' and 'C' for access to required field 'C:Section.name<String>'.
                        No lookups found for type 'Section' in schema 'C'.

            Unable to access the field 'Category.name' on path 'A:Query.productById<Product> -> B:Product.category<Category>'.
              Unable to transition between schemas 'B' and 'C' for access to field 'C:Category.name<String>'.
                No lookups found for type 'Category' in schema 'C'.

            Unable to access the field 'Section.name' on path 'A:Query.productById<Product> -> B:Product.section<Section>'.
              Unable to transition between schemas 'B' and 'C' for access to field 'C:Section.name<String>'.
                No lookups found for type 'Section' in schema 'C'.

            Unable to access the field 'Product.title' on path 'B:Query.productById<Product>'.
              Unable to satisfy the requirement '{ category { name } section { name } }' on field 'A:Product.title<String>'.
                Unable to satisfy the requirement 'category { name }'.
                  Unable to access the required field 'Product.category' on path 'B:Query.productById<Product>'.
                    Unable to access the required field 'Category.name' on path 'B:Query.productById<Product> -> B:Product.category<Category>'.
                      Unable to transition between schemas 'B' and 'C' for access to required field 'C:Category.name<String>'.
                        No lookups found for type 'Category' in schema 'C'.
                Unable to satisfy the requirement 'section { name }'.
                  Unable to access the required field 'Product.section' on path 'B:Query.productById<Product>'.
                    Unable to access the required field 'Section.name' on path 'B:Query.productById<Product> -> B:Product.section<Section>'.
                      Unable to transition between schemas 'B' and 'C' for access to required field 'C:Section.name<String>'.
                        No lookups found for type 'Section' in schema 'C'.

            Unable to access the field 'Category.name' on path 'B:Query.productById<Product> -> B:Product.category<Category>'.
              Unable to transition between schemas 'B' and 'C' for access to field 'C:Category.name<String>'.
                No lookups found for type 'Category' in schema 'C'.

            Unable to access the field 'Section.name' on path 'B:Query.productById<Product> -> B:Product.section<Section>'.
              Unable to transition between schemas 'B' and 'C' for access to field 'C:Section.name<String>'.
                No lookups found for type 'Section' in schema 'C'.
            """);
    }
}

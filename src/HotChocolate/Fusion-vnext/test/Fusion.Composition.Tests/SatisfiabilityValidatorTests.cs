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
                    id: ID! @shareable
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
                    id: ID! @shareable
                    membershipStatus: String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

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
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.sku' on path 'B:Query.productById<Product>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.sku<String>'.
                Unable to satisfy the requirement '{ id sku }' for lookup 'productByIdSku' in schema 'A'.
                  Unable to satisfy the requirement 'sku'.
                    Unable to access the required field 'Product.sku' on path 'B:Query.productById<Product>'.
                        No other schemas contain the field 'Product.sku'.

            Unable to access the field 'Product.name' on path 'B:Query.productById<Product>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.name<String>'.
                Unable to satisfy the requirement '{ id sku }' for lookup 'productByIdSku' in schema 'A'.
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
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.sku' on path 'A:Query.productById<Product>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.sku<String>'.
                Unable to satisfy the requirement '{ sku }' for lookup 'productBySku' in schema 'B'.
                  Unable to satisfy the requirement 'sku'.
                    Unable to access the required field 'Product.sku' on path 'A:Query.productById<Product>'.
                      No other schemas contain the field 'Product.sku'.

            Unable to access the field 'Product.stock' on path 'A:Query.productById<Product>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.stock<Int>'.
                Unable to satisfy the requirement '{ sku }' for lookup 'productBySku' in schema 'B'.
                  Unable to satisfy the requirement 'sku'.
                    Unable to access the required field 'Product.sku' on path 'A:Query.productById<Product>'.
                      No other schemas contain the field 'Product.sku'.

            Unable to access the field 'Product.id' on path 'B:Query.productBySku<Product>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.id<ID>'.
                Unable to satisfy the requirement '{ id }' for lookup 'productById' in schema 'A'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Product.id' on path 'B:Query.productBySku<Product>'.
                      No other schemas contain the field 'Product.id'.

            Unable to access the field 'Product.description' on path 'B:Query.productBySku<Product>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.description<String>'.
                Unable to satisfy the requirement '{ id }' for lookup 'productById' in schema 'A'.
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

                type Address @shareable {
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

                type Address @shareable {
                    street: String
                    city: String
                    country: String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

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

                type Category @key(fields: "id") {
                    id: ID!
                    description: String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Category.id' on path 'A:Query.productById<Product> -> A:Product.category<Category>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Category.id<ID>'.
                Unable to satisfy the requirement '{ id }' for lookup 'categoryById' in schema 'B'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Category.id' on path 'A:Product.category<Category>'.
                        No other schemas contain the field 'Category.id'.

            Unable to access the field 'Category.description' on path 'A:Query.productById<Product> -> A:Product.category<Category>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Category.description<String>'.
                Unable to satisfy the requirement '{ id }' for lookup 'categoryById' in schema 'B'.
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
    // https://github.com/graphql-hive/federation-gateway-audit/tree/main/src/test-suites/abstract-types
    public void AbstractTypes()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    publisherTypeById(id: ID!): PublisherType @lookup @inaccessible # Added
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
    // https://github.com/graphql-hive/federation-gateway-audit/tree/main/src/test-suites/child-type-mismatch
    public void ChildTypeMismatch()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type User {
                    id: ID @shareable
                }

                type Query {
                    users: [User!]!
                }
                """,
                """
                # Schema B
                union Account = User | Admin

                type User @key(fields: "id") {
                    id: ID!
                    name: String
                    similarAccounts: [Account!]!
                }

                type Admin {
                    id: ID
                    name: String @shareable
                    similarAccounts: [Account!]!
                }

                type Query {
                    accounts: [Account!]!
                    userById(id: ID!): User @lookup @inaccessible # Added
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
    // https://github.com/graphql-hive/federation-gateway-audit/tree/main/src/test-suites/complex-entity-call
    public void ComplexEntityCall()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query { # Added
                    productById(id: String!): Product @lookup @inaccessible
                    productByIdAndPid(id: String!, pid: String!): Product @lookup @inaccessible
                }

                type Product @key(fields: "id") @key(fields: "id pid") {
                    id: String!
                    pid: String!
                }
                """,
                """
                # Schema B
                type Query { # Added
                    productListByProductsIdAndPid(
                        key: [ProductIdAndPidInput!]! @is(field: "products[{ id, pid }]")
                    ): ProductList @lookup @inaccessible
                    productByIdAndPid(id: String!, pid: String): Product @lookup @inaccessible
                }

                input ProductIdAndPidInput { # Added
                    id: String!
                    pid: String
                }

                type ProductList @key(fields: "products { id pid }") {
                    products: [Product!]!
                    first: Product @shareable
                    selected: Product @shareable
                }

                type Product @key(fields: "id pid") {
                    id: String!
                    pid: String
                }
                """,
                """
                # Schema C
                type Query { # Added
                    productListByProductsIdAndPidAndCategoryAndSelected(
                        products: [ProductIdAndPidAndCategoryInput!]!
                            @is(field: "products[{ id, pid, category: category.{ id, tag } }]")
                        selectedId: String! @is(field: "selected.id")
                    ): ProductList @lookup @inaccessible
                    productByIdAndPidAndCategory(
                        id: String!
                        pid: String
                        category: CategoryIdAndTagInput! @is(field: "category.{ id, tag }")
                    ): Product @lookup @inaccessible
                    categoryByIdAndTag(id: String!, tag: String): Category @lookup @inaccessible
                }

                input ProductIdAndPidAndCategoryInput {
                    id: String!
                    pid: String
                    category: CategoryIdAndTagInput!
                }

                input CategoryIdAndTagInput {
                    id: String!
                    tag: String
                }

                type ProductList
                    @key(fields: "products { id pid category { id tag } } selected { id }") {
                    products: [Product!]!
                    first: Product @shareable
                    selected: Product @shareable
                }

                type Product @key(fields: "id pid category { id tag }") {
                    id: String!
                    price: Price
                    pid: String
                    category: Category
                }

                type Category @key(fields: "id tag") {
                    id: String!
                    tag: String
                }

                type Price {
                    price: Float!
                }
                """,
                """
                # Schema D
                type Query {
                    topProducts: ProductList!
                    productListByProductsId(
                        products: [ProductIdInput!]! @is(field: "products[id]")
                    ): ProductList @lookup @inaccessible # Added
                    productById(id: String!): Product @lookup @inaccessible # Added
                    categoryById(id: String!): Category @lookup @inaccessible # Added
                }

                input ProductIdInput {
                    id: String!
                }

                type ProductList @key(fields: "products { id }") {
                    products: [Product!]!
                }

                type Product @key(fields: "id") {
                    id: String! # Removed the @external directive, which works differently.
                    category: Category @shareable
                }

                type Category @key(fields: "id") {
                    mainProduct: Product! @shareable
                    id: String!
                    tag: String @shareable
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
    // https://github.com/graphql-hive/federation-gateway-audit/tree/main/src/test-suites/keys-mashup
    public void KeysMashup()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    aById(id: ID!): A @lookup @inaccessible # Added
                }

                type A
                    @key(fields: "id", resolvable: true)
                    @key(fields: "pId", resolvable: false)
                    @key(fields: "compositeId { one two }", resolvable: false)
                    @key(fields: "id compositeId { two three }", resolvable: false) {
                    id: ID!
                    pId: ID!
                    compositeId: CompositeID!
                    name: String!
                }

                type CompositeID {
                    one: ID!
                    two: ID!
                    three: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    b: B
                    aByIdAndCompositeId(
                        id: ID!
                        compositeId: CompositeIDInput!
                    ): A @lookup @inaccessible # Added
                }

                input CompositeIdInput {
                    two: ID!
                    three: ID!
                }

                type B @key(fields: "id") {
                    id: ID!
                    a: [A!]!
                }

                type A
                    @key(fields: "compositeId { one two }", resolvable: false)
                    @key(fields: "id compositeId { two three }", resolvable: true)
                    @key(fields: "pId", resolvable: false)
                    @key(fields: "id", resolvable: false) {
                    id: ID!
                    pId: ID!
                    compositeId: CompositeID!
                    name: String! @external
                    nameInB: String! @require(fields: "name")
                }

                type CompositeID {
                    one: ID!
                    two: ID!
                    three: ID!
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
    // https://github.com/graphql-hive/federation-gateway-audit/tree/main/src/test-suites/mutations
    public void Mutations()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Mutation {
                    addProduct(input: AddProductInput!): Product!
                    multiply(by: Int!, requestId: String!): Int!
                }

                type Query {
                    product(id: ID!): Product!
                    products: [Product!]!
                }

                input AddProductInput {
                    name: String!
                    price: Float!
                }

                type Product @key(fields: "id") {
                    id: ID!
                    name: String!
                    price: Float!
                }
                """,
                """
                # Schema B
                type Product @key(fields: "id") {
                    id: ID!
                    price: Float! @external
                    isExpensive: Boolean! @require(fields: "price")
                    isAvailable: Boolean!
                }

                type Query { # Added
                    productById(id: ID!): Product @lookup @inaccessible
                }

                type Mutation {
                    delete(requestId: String!): Int!
                }
                """,
                """
                # Schema C
                type Mutation {
                    add(num: Int!, requestId: String!): Int!
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
    // https://github.com/graphql-hive/federation-gateway-audit/tree/main/src/test-suites/node
    public void Node()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productNode: Node
                    categoryNode: Node
                }

                interface Node {
                    id: ID!
                }

                type Product implements Node @key(fields: "id") {
                    id: ID!
                }

                type Category implements Node @key(fields: "id") {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query { # Added
                    node(id: ID!): Node @lookup @inaccessible
                }

                interface Node {
                    id: ID!
                }

                type Product implements Node @key(fields: "id") @shareable {
                    id: ID!
                    name: String!
                    price: Float!
                }

                type Category implements Node @key(fields: "id") {
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
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

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
    public void InterfaceLookup_Implementing_Type_Is_Not_Implementing_Interface_In_Specific_Source_Schema()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    animalById(id: ID!): Animal @lookup
                    myCat: Cat
                }

                type Cat {
                    id: ID!
                    age: Int!
                }

                type Dog implements Animal {
                    id: ID!
                    age: Int!
                }

                interface Animal {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    # it's important the name of the lookup here is different,
                    # otherwise it still works, since the path is the same.
                    animal(id: ID!): Animal @lookup
                    myDog: Dog
                }

                type Cat implements Animal {
                    id: ID!
                    name: String!
                }

                type Dog {
                    id: ID!
                    name: String!
                }

                interface Animal {
                    id: ID!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.False(result.IsSuccess);
        string.Join("\n\n", log.Select(e => e.Message))
            .MatchInlineSnapshot(
                """
                Unable to access the field 'Dog.name' on path 'A:Query.animalById<Animal>'.
                  Unable to transition between schemas 'A' and 'B' for access to field 'B:Dog.name<String>'.
                    No lookups found for type 'Dog' in schema 'B'.

                Unable to access the field 'Cat.age' on path 'B:Query.animal<Animal>'.
                  Unable to transition between schemas 'B' and 'A' for access to field 'A:Cat.age<Int>'.
                    No lookups found for type 'Cat' in schema 'A'.
                """);
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
    public void OneOf()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                schema {
                    query: Query
                }

                type Query {
                    brand(by: BrandByInput @is(field: "{ id } | { key }")): Brand @lookup
                }

                type Brand @key(fields: "id") {
                    id: Int!
                    key: String!
                    name: String!
                }

                input BrandByInput @oneOf {
                    id: Int
                    key: String
                }
                """,
                """
                # Schema B
                schema {
                    query: Query
                }

                type Query {
                    products: [Product]
                }

                type Brand @key(fields: "id") {
                    id: Int!
                }

                type Product @key(fields: "id") {
                    id: Int!
                    name: String!
                    brand: Brand
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
    public void UnionLookup_Member_Type_Is_Not_Member_In_Specific_Source_Schema()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    animalById(id: ID!): Animal @lookup
                }

                type Cat {
                    id: ID!
                    age: Int!
                }

                type Dog {
                    id: ID!
                    age: Int!
                }

                union Animal = Dog
                """,
                """
                # Schema B
                type Query {
                    # it's important the name of the lookup here is different,
                    # otherwise it still works, since the path is the same.
                    animal(id: ID!): Animal @lookup
                }

                type Cat {
                    id: ID!
                    name: String!
                }

                type Dog {
                    id: ID!
                    name: String!
                }

                union Animal = Cat
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.False(result.IsSuccess);
        string.Join("\n\n", log.Select(e => e.Message))
            .MatchInlineSnapshot(
                """
                Unable to access the field 'Dog.name' on path 'A:Query.animalById<Animal>'.
                  Unable to transition between schemas 'A' and 'B' for access to field 'B:Dog.name<String>'.
                    No lookups found for type 'Dog' in schema 'B'.

                Unable to access the field 'Cat.age' on path 'B:Query.animal<Animal>'.
                  Unable to transition between schemas 'B' and 'A' for access to field 'A:Cat.age<Int>'.
                    No lookups found for type 'Cat' in schema 'A'.
                """);
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
                    title(description: String @require(field: "{ name, price }")): String
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
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

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
                        input: String @require(field: "{ a: category.name, b: section.name }")
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
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

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
                    Unable to access the required field 'Category.name' on path 'A:Query.productById<Product> -> B:Product.category<Category>'.
                      Unable to transition between schemas 'B' and 'C' for access to required field 'C:Category.name<String>'.
                        No lookups found for type 'Category' in schema 'C'.
                Unable to satisfy the requirement 'section { name }'.
                  Unable to access the required field 'Product.section' on path 'A:Query.productById<Product>'.
                    Unable to access the required field 'Section.name' on path 'A:Query.productById<Product> -> B:Product.section<Section>'.
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
            """);
    }

    [Fact]
    public void Type_Without_Lookup_But_Path_Matches_Up_To_Root()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    viewer: Viewer
                }

                type Viewer {
                    firstName: String!
                }
                """,
                """
                # Schema B
                type Query {
                    viewer: Viewer
                }

                type Viewer {
                    lastName: String!
                }
                """,
                """
                # Schema C
                type Query {
                    viewer: Viewer
                }

                type Viewer {
                    middleName: String!
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
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Type_Without_Lookup_But_Path_Matches_Up_To_Root_With_Requirement()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    viewer: Viewer
                }

                type Viewer {
                    firstName: String!
                }
                """,
                """
                # Schema B
                type Query {
                    viewer: Viewer
                }

                type Viewer {
                    fullName(firstName: String! @require(field: "{ firstName }")): String!
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
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Type_Without_Lookup_Not_All_Schemas_Share_Path_Up_To_Root()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    viewer: Viewer
                }

                type Viewer {
                    firstName: String!
                }
                """,
                """
                # Schema B
                type Query {
                    other: Viewer
                }

                type Viewer {
                    lastName: String!
                }
                """,
                """
                # Schema C
                type Query {
                    viewer: Viewer
                }

                type Viewer {
                    middleName: String!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Viewer.lastName' on path 'A:Query.viewer<Viewer>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Viewer.lastName<String>'.
                No lookups found for type 'Viewer' in schema 'B'.

            Unable to access the field 'Viewer.lastName' on path 'C:Query.viewer<Viewer>'.
              Unable to transition between schemas 'C' and 'B' for access to field 'B:Viewer.lastName<String>'.
                No lookups found for type 'Viewer' in schema 'B'.

            Unable to access the field 'Viewer.firstName' on path 'B:Query.other<Viewer>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Viewer.firstName<String>'.
                No lookups found for type 'Viewer' in schema 'A'.

            Unable to access the field 'Viewer.middleName' on path 'B:Query.other<Viewer>'.
              Unable to transition between schemas 'B' and 'C' for access to field 'C:Viewer.middleName<String>'.
                No lookups found for type 'Viewer' in schema 'C'.
            """);
    }

    [Fact]
    public void Type_Without_Lookup_Not_All_Schemas_Share_Path_Up_To_Root_With_Requirement()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    viewer: Viewer
                }

                type Viewer {
                    fullName(firstName: String! @require(field: "{ firstName }")): String!
                }
                """,
                """
                # Schema B
                type Query {
                    other: Viewer
                }

                type Viewer {
                    firstName: String!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Viewer.fullName' on path 'A:Query.viewer<Viewer>'.
              Unable to satisfy the requirement '{ firstName }' on field 'A:Viewer.fullName<String>'.
                Unable to satisfy the requirement 'firstName'.
                  Unable to access the required field 'Viewer.firstName' on path 'A:Query.viewer<Viewer>'.
                    Unable to transition between schemas 'A' and 'B' for access to required field 'B:Viewer.firstName<String>'.
                      No lookups found for type 'Viewer' in schema 'B'.

            Unable to access the field 'Viewer.firstName' on path 'A:Query.viewer<Viewer>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Viewer.firstName<String>'.
                No lookups found for type 'Viewer' in schema 'B'.

            Unable to access the field 'Viewer.fullName' on path 'B:Query.other<Viewer>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Viewer.fullName<String>'.
                No lookups found for type 'Viewer' in schema 'A'.
            """);
    }

    [Fact]
    public void Type_Without_Lookup_But_Parent_Has_Lookup()
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
                    availability: ProductAvailability!
                }

                type ProductAvailability {
                    date: String
                }
                """,
                """
                # Schema B
                type Query {
                    node(id: ID!): Node @lookup
                }

                interface Node {
                    id: ID!
                }

                type Product implements Node {
                    id: ID!
                    availability: ProductAvailability!
                }

                type ProductAvailability {
                    quantityOnHand: Int
                }
                """,
                """
                # Schema C
                type Query {
                    product(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    availability: ProductAvailability!
                }

                type ProductAvailability {
                    details: String
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
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void IgnoredNonAccessibleFields()
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
                        input: String @require(field: "{ a: category.name, b: section.name }")
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
        var options = new SatisfiabilityOptions
        {
            IgnoredNonAccessibleFields =
            {
                {
                    "Product.title",
                    ["A:Query.productById<Product>", "B:Query.productById<Product>"]
                },
                {
                    "Section.name",
                    ["A:Query.productById<Product> -> B:Product.section<Section>"]
                }
            },
            IncludeSatisfiabilityPaths = true
        };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Category.name' on path 'A:Query.productById<Product> -> B:Product.category<Category>'.
              Unable to transition between schemas 'B' and 'C' for access to field 'C:Category.name<String>'.
                No lookups found for type 'Category' in schema 'C'.
            """);
    }

    [Fact]
    public void RootRequirement()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    optionalField: String
                }
                """,
                """
                # Schema B
                type Query {
                    requiredField(value: String @require(field: "optionalField")): String!
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

    [Theory]
    [MemberData(nameof(GlobalObjectIdentificationExamplesData))]
    public void GlobalObjectIdentification_Examples(string[] sdl, bool success, string? logs = null)
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(sdl),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false, EnableGlobalObjectIdentification = true });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.Equal(success, result.IsSuccess);

        if (!success)
        {
            string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(logs!);
        }
    }

    public static TheoryData<string[], bool, string?> GlobalObjectIdentificationExamplesData()
    {
        return new TheoryData<string[], bool, string?>
        {
            // A source schema doesn't have any lookup for a type implementing Node
            {
                [
                    """
                    # Schema A
                    type Query {
                        myCat: Cat
                    }

                    interface Node {
                        id: ID!
                    }

                    type Cat implements Node {
                        id: ID!
                        name: String!
                    }
                    """
                ],
                false,
                "Type 'Cat' implements the 'Node' interface, but no source schema provides a non-internal 'Query.node<Node>' lookup field for this type."
            },
            // A source schema has a lookup but not by ID for a type implementing Node
            {
                [
                    """
                    # Schema A
                    type Query {
                        catByName(name: String!): Cat @lookup
                    }

                    interface Node {
                        id: ID!
                    }

                    type Cat implements Node {
                        id: ID!
                        name: String!
                    }
                    """
                ],
                false,
                "Type 'Cat' implements the 'Node' interface, but no source schema provides a non-internal 'Query.node<Node>' lookup field for this type."
            },
            // A source schema has a lookup by ID but not the Query.node field for a type implementing Node
            {
                [
                    """
                    # Schema A
                    type Query {
                        catById(id: ID!): Cat @lookup
                    }

                    interface Node {
                        id: ID!
                    }

                    type Cat implements Node {
                        id: ID!
                        name: String!
                    }
                    """,
                    """
                    # Schema B
                    type Query {
                        node(id: ID!): Node @lookup
                    }

                    interface Node {
                        id: ID!
                    }

                    type Dog implements Node {
                        id: ID!
                        age: Int!
                    }
                    """
                ],
                false,
                "Type 'Cat' implements the 'Node' interface, but no source schema provides a non-internal 'Query.node<Node>' lookup field for this type."
            },
            // A source schema has a lookup by ID, but it's internal
            {
                [
                    """
                    # Schema A
                    type Query {
                        node(id: ID!): Node @lookup @internal
                        myDog: Dog
                    }

                    interface Node {
                        id: ID!
                    }

                    type Dog implements Node {
                        id: ID!
                        age: Int!
                    }
                    """
                ],
                false,
                "Type 'Dog' implements the 'Node' interface, but no source schema provides a non-internal 'Query.node<Node>' lookup field for this type."
            },
            // Type implements Node in another source schema than where a lookup for Node is
            {
                [
                    """
                    # Schema A
                    type Query {
                        node(id: ID!): Node @lookup
                    }

                    interface Node {
                        id: ID!
                    }

                    type Cat implements Node {
                        id: ID!
                    }

                    # Node and Dog are both in this schema, but Dog doesn't implement Node
                    type Dog {
                        id: ID!
                        age: Int!
                    }
                    """,
                    """
                    # Schema B
                    type Query {
                        topProduct: Product
                    }

                    interface Node {
                        id: ID!
                    }

                    type Dog implements Node {
                        id: ID!
                        name: String!
                    }
                    """
                ],
                false,
                "Type 'Dog' implements the 'Node' interface, but no source schema provides a non-internal 'Query.node<Node>' lookup field for this type."
            },
            // A source schema is missing a lookup for an exclusive field
            {
                [
                    """
                    # Schema A
                    type Query {
                        node(id: ID!): Node @lookup
                    }

                    interface Node {
                        id: ID!
                    }

                    type Cat implements Node {
                        id: ID!
                    }
                    """,
                    """
                    # Schema B
                    type Cat {
                        name: String!
                    }
                    """
                ],
                false,
                """
                Unable to access the field 'Cat.name' on path '*:Query.node<Node> -> A:Query.node<Node>'.
                  Unable to transition between schemas 'A' and 'B' for access to field 'B:Cat.name<String>'.
                    No lookups found for type 'Cat' in schema 'B'.
                """
            },
            // A source schema can't transition to another one
            {
                [
                    """
                    # Schema A
                    type Query {
                        catByName(name: String!): Cat @lookup
                    }

                    interface Node {
                        id: ID!
                    }

                    type Cat implements Node {
                        id: ID!
                        name: String!
                    }
                    """,
                    """
                    # Schema B
                    type Query {
                        node(id: ID!): Node @lookup
                    }

                    interface Node {
                        id: ID!
                    }

                    type Cat implements Node {
                        id: ID!
                        age: Int!
                    }
                    """
                ],
                false,
                """
                Unable to access the field 'Cat.name' on path '*:Query.node<Node> -> B:Query.node<Node>'.
                  Unable to transition between schemas 'B' and 'A' for access to field 'A:Cat.name<String>'.
                    Unable to satisfy the requirement '{ name }' for lookup 'catByName' in schema 'A'.
                      Unable to satisfy the requirement 'name'.
                        Unable to access the required field 'Cat.name' on path 'B:Query.node<Node>'.
                          No other schemas contain the field 'Cat.name'.
                """
            },
            {
                [
                    """
                    # Schema A
                    type Query {
                        node(id: ID!): Node @lookup
                    }

                    interface Node {
                        id: ID!
                    }

                    type Cat implements Node {
                        id: ID!
                        name: String!
                    }
                    """
                ],
                true,
                null
            },
            // A source schema is missing a lookup, but the fields it's contributing aren't exclusive
            {
                [
                    """
                    # Schema A
                    type Query {
                        node(id: ID!): Node @lookup
                    }

                    interface Node {
                        id: ID!
                    }

                    type Cat implements Node {
                        id: ID!
                        name: String!
                    }
                    """,
                    """
                    # Schema B
                    type Cat {
                        id: ID!
                    }
                    """
                ],
                true,
                null
            },
            // Same case as above, just the order of schemas is different
            {
                [
                    """
                    # Schema A
                    type Cat {
                        id: ID!
                    }
                    """,
                    """
                    # Schema B
                    type Query {
                        node(id: ID!): Node @lookup
                    }

                    interface Node {
                        id: ID!
                    }

                    type Cat implements Node {
                        id: ID!
                        name: String!
                    }
                    """
                ],
                true,
                null
            }
        };
    }
}

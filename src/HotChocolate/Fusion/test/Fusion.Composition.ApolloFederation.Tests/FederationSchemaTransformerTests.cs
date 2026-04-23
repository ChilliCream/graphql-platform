namespace HotChocolate.Fusion.ApolloFederation;

public sealed class FederationSchemaTransformerTests
{
    [Fact]
    public void Transform_SimpleEntity()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
            }

            type Query {
              product(id: ID!): Product
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_CompositeKey()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "sku package") {
              sku: String!
              package: String!
              name: String
            }

            type Query {
              products: [Product]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_MultipleKeys()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "id") @key(fields: "sku package") {
              id: ID!
              sku: String!
              package: String!
              name: String
            }

            type Query {
              products: [Product]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_RequiresDirective()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@requires", "@external"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
              price: Float @external
              weight: Float @external
              shippingEstimate: Float @requires(fields: "price weight")
            }

            type Query {
              product(id: ID!): Product
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @requires(fields: FieldSet!) on FIELD_DEFINITION
            directive @external on FIELD_DEFINITION
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_Should_Generate_Nullable_RequireArgument_When_SourceField_Is_Nullable()
    {
        // arrange: 'price' and 'weight' on the owning type are nullable so the
        // generated '@require' arguments must mirror that nullability. Wrapping
        // them in NonNull would cause post-merge validation to reject the
        // composition because the composed schema's field types stay nullable.
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@requires", "@external"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
              price: Float @external
              weight: Float @external
              shippingEstimate: Float @requires(fields: "price weight")
            }

            type Query {
              product(id: ID!): Product
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @requires(fields: FieldSet!) on FIELD_DEFINITION
            directive @external on FIELD_DEFINITION
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Contains("price: Float @require(field: \"price\")", result.Value);
        Assert.Contains("weight: Float @require(field: \"weight\")", result.Value);
        Assert.DoesNotContain("price: Float!", result.Value);
        Assert.DoesNotContain("weight: Float!", result.Value);
    }

    [Fact]
    public void Transform_ProvidesDirective()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@provides"]) {
              query: Query
            }

            type User @key(fields: "id") {
              id: ID!
              username: String
              totalProductsCreated: Int
            }

            type Review {
              body: String
              author: User @provides(fields: "username")
            }

            type Query {
              reviews: [Review]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = User

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @provides(fields: FieldSet!) on FIELD_DEFINITION
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_ExternalDirective()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@external"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
              price: Float @external
            }

            type Query {
              products: [Product]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @external on FIELD_DEFINITION
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_NonResolvableKey()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "id", resolvable: false) {
              id: ID!
              name: String
            }

            type Query {
              products: [Product]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_FullIntegration()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@requires", "@provides", "@external"]) {
              query: Query
            }

            type Product @key(fields: "id") @key(fields: "sku package") {
              id: ID!
              sku: String!
              package: String!
              name: String
              price: Float
              weight: Float
              inStock: Boolean
              createdBy: User @provides(fields: "totalProductsCreated")
            }

            type User @key(fields: "id") {
              id: ID!
              username: String @external
              totalProductsCreated: Int
            }

            type Review {
              body: String
              author: User
            }

            type Query {
              product(id: ID!): Product
              reviews: [Review]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product | User

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @requires(fields: FieldSet!) on FIELD_DEFINITION
            directive @provides(fields: FieldSet!) on FIELD_DEFINITION
            directive @external on FIELD_DEFINITION
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_KeyResolvableArgument()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "id", resolvable: true) {
              id: ID!
              name: String
            }

            type Query {
              products: [Product]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_NonResolvableAndResolvableKeys()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "id") @key(fields: "sku", resolvable: false) {
              id: ID!
              sku: String!
              name: String
            }

            type Query {
              products: [Product]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_InterfaceObject_Should_ReturnError()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@interfaceObject"]) {
              query: Query
            }

            type Product @key(fields: "id") @interfaceObject {
              id: ID!
              name: String
            }

            type Query {
              products: [Product]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @interfaceObject on OBJECT
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsFailure);
        Assert.Contains(
            result.Errors,
            e => e.Message.Contains("@interfaceObject"));
    }

    [Fact]
    public void Transform_FederationV1_Should_ReturnError()
    {
        // arrange — no @link directive means v1
        const string federationSdl =
            """
            type Product @key(fields: "id") {
              id: ID!
              name: String
            }

            type Query {
              product(id: ID!): Product
            }

            directive @key(fields: String!) repeatable on OBJECT | INTERFACE
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsFailure);
        Assert.Contains(
            result.Errors,
            e => e.Message.Contains("v1"));
    }

    [Fact]
    public void Transform_InvalidSdl_Should_ReturnParseError()
    {
        // arrange
        const string federationSdl = "this is not valid graphql }{][";

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsFailure);
        Assert.Contains(
            result.Errors,
            e => e.Message.Contains("parse"));
    }

    [Fact]
    public void Transform_EmptyString_Should_ThrowArgumentException()
    {
        // arrange & act & assert
        Assert.Throws<ArgumentException>(
            () => FederationSchemaTransformer.Transform(string.Empty));
    }

    [Fact]
    public void Transform_NestedObjectKey()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Article @key(fields: "metadata { id }") {
              metadata: ArticleMetadata!
              title: String!
            }

            type ArticleMetadata {
              id: ID!
              author: String
            }

            type Query {
              article: Article
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Article

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_NestedListKey()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type ProductList @key(fields: "products { id }") {
              products: [Product!]!
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Query {
              topProducts: ProductList!
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = ProductList | Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_DeeplyNestedListKey()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@shareable"]) {
              query: Query
            }

            type ProductList
              @key(fields: "products { id pid category { id tag } } selected { id }") {
              products: [Product!]!
              first: Product @shareable
              selected: Product @shareable
            }

            type Product @key(fields: "id pid category { id tag }") {
              id: String!
              pid: String
              category: Category
            }

            type Category @key(fields: "id tag") {
              id: String!
              tag: String
            }

            type Query {
              topProducts: ProductList!
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = ProductList | Product | Category

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @shareable on FIELD_DEFINITION | OBJECT
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }
}

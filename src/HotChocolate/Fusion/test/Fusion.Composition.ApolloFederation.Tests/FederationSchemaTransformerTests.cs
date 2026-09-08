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
    public void Transform_FieldSetReferencedByUserDirective_IsKept()
    {
        // arrange
        // FieldSet is exported vocabulary; a user-defined directive uses it as an argument type,
        // so it must survive the removal of federation infrastructure
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String @audit(fields: "id")
            }

            type Query {
              product(id: ID!): Product
            }

            scalar FieldSet

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @audit(fields: FieldSet!) on FIELD_DEFINITION
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Contains("scalar FieldSet", result.Value);
        Assert.Contains("@audit", result.Value);
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
    public void Transform_Should_GenerateSingleLookup_When_SameKeyIsRepeatedOnTypeAndExtension()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Item @key(fields: "id") {
              id: ID!
              name: String
            }

            extend type Item @key(fields: "id") {
              quantity: Int
            }

            type Query {
              item(id: ID!): Item
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Item

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
    public void Transform_Should_GenerateSingleLookup_When_KeysDifferOnlyInWhitespaceOrFieldOrder()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Product @key(fields: "sku package") @key(fields: "sku      package") @key(fields: "package sku") {
              sku: String!
              package: String!
              name: String
            }

            type Order @key(fields: "meta { id region }") @key(fields: "meta { region id }") {
              meta: OrderMeta!
              total: Int
            }

            type OrderMeta {
              id: ID!
              region: String!
            }

            type Query {
              products: [Product]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Product | Order

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
    public void Transform_Should_KeepBothKeys_When_SameFieldsDifferOnlyInResolvable()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Item @key(fields: "a b", resolvable: false) @key(fields: "b a") {
              a: ID!
              b: String!
              name: String
            }

            type Query {
              items: [Item]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Item

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
    public void Transform_Should_GenerateSingleLookup_When_ListSegmentKeyIsRepeated()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Item @key(fields: "products { id }") @key(fields: "products { id }") {
              id: ID!
              products: [Product!]!
            }

            type Product {
              id: ID!
            }

            type Query {
              items: [Item]
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Item

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
    public void Transform_Should_UseListSyntax_When_RequiresPathCrossesListIntermediate()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@requires", "@external"]) {
              query: Query
            }

            type Order @key(fields: "id") {
              id: ID!
              info: Info @external
              summary: Boolean @requires(fields: "info { lines { sku } }")
            }

            type Info {
              lines: [Line]
            }

            type Line @key(fields: "sku") {
              sku: ID!
            }

            type Query {
              order(id: ID!): Order
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type _Service { sdl: String! }

            union _Entity = Order | Line

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
    public void Transform_Should_PreserveOnlyConditionedUnionProvidesFields()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(
                url: "https://specs.apollo.dev/federation/v2.6"
                import: ["@external", "@key", "@provides", "@shareable"]) {
              query: Query
            }

            type Query {
              media: [Media] @shareable @provides(fields: "... on Book { title }")
              wrapper: Wrapper @provides(fields: "media { ... on Book { subtitle } }")
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type Wrapper {
              media: Media
            }

            union Media = Book | Movie

            type Book @key(fields: "id") {
              id: ID!
              subtitle: String @external
              title: String @external
            }

            type Movie @key(fields: "id") {
              id: ID!
              subtitle: String @external
              title: String @external
            }

            type _Service { sdl: String! }

            union _Entity = Book | Movie

            scalar FieldSet
            scalar _Any

            directive @external on FIELD_DEFINITION
            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @provides(fields: FieldSet!) on FIELD_DEFINITION
            directive @shareable repeatable on OBJECT | FIELD_DEFINITION
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
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
    public void Transform_Should_PreserveExternalKeyField_When_GeneratingLookup()
    {
        // arrange
        const string federationSdl =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@external"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID! @external
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
    public void Transform_InterfaceObject_Should_Preserve()
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
        // @interfaceObject maps 1:1 to the native construct, so it is translated rather than
        // rejected: the transform keeps the directive and its application on the stand-in.
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
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

    // Ruling repo-ftx: an Apollo @policy(policies:) application on an object type must be
    // translated into Fusion's @policy(names:) shape (same DNF list-of-list shape, no onDenied
    // so the schema default applies) rather than being dropped by the federation import.
    [Fact]
    public void Transform_PolicyDirective_OnObjectType()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"])
              @link(url: "https://specs.apollo.dev/policy/v0.1", import: ["@policy"]) {
              query: Query
            }

            type Product @key(fields: "id") @policy(policies: [["internal"], ["support"]]) {
              id: ID!
              name: String
            }

            type Query {
              product(id: ID!): Product
            }

            scalar FieldSet
            scalar federation__Policy

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @policy(policies: [[federation__Policy!]!]!) repeatable
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
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

    // Ruling repo-ftx: the same translation applies to an Apollo @policy(policies:) application
    // on a field.
    [Fact]
    public void Transform_PolicyDirective_OnField()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"])
              @link(url: "https://specs.apollo.dev/policy/v0.1", import: ["@policy"]) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
              internalNotes: String @policy(policies: [["internal"]])
            }

            type Query {
              product(id: ID!): Product
            }

            scalar FieldSet
            scalar federation__Policy

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @policy(policies: [[federation__Policy!]!]!) repeatable
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
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

    // Ruling repo-ftx: @policy must still be recognized and translated when the policy spec is
    // imported under a renamed local directive name, e.g.
    // @link(import: [{name: "@policy", as: "@authz"}]). The rewritten application still carries
    // Fusion's canonical @policy name regardless of the source-schema alias.
    [Fact]
    public void Transform_PolicyDirective_RenamedImport()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"])
              @link(
                url: "https://specs.apollo.dev/policy/v0.1"
                import: [{ name: "@policy", as: "@authz" }]
              ) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String @authz(policies: [["internal"]])
            }

            type Query {
              product(id: ID!): Product
            }

            scalar FieldSet
            scalar federation__Policy
            scalar link__Import

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [link__Import]) repeatable on SCHEMA
            directive @authz(policies: [[federation__Policy!]!]!) repeatable
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
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

    // Ruling repo-ftx: Fusion's canonical @policy directive only allows the OBJECT and
    // FIELD_DEFINITION locations. An Apollo @policy(policies:) application on a scalar or enum
    // type cannot be carried over by the rewrite, so it must fail composition loudly instead of
    // being silently dropped.
    [Fact]
    public void Transform_PolicyDirective_OnEnumOrScalar()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"])
              @link(url: "https://specs.apollo.dev/policy/v0.1", import: ["@policy"]) {
              query: Query
            }

            type Query {
              color: Color
              money: Money
            }

            enum Color @policy(policies: [["internal"]]) {
              RED
              BLUE
            }

            scalar Money @policy(policies: [["finance"]])

            scalar FieldSet
            scalar federation__Policy

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @policy(policies: [[federation__Policy!]!]!) repeatable
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.False(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(string.Join(Environment.NewLine, result.Errors.Select(e => e.Message)), "Errors")
            .MatchMarkdownSnapshot();
    }

    // Ruling repo-ctf.24 (comment 704): an Apollo @authenticated application on an object type
    // must be translated into a single Fusion @policy(names: [["fusion.authenticated"]]) with
    // onDenied: ERROR, rather than being silently dropped by the federation import.
    [Fact]
    public void Transform_AuthenticatedDirective_OnObjectType()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.6"
                import: ["@key", "@authenticated"]
              ) {
              query: Query
            }

            type Product @key(fields: "id") @authenticated {
              id: ID!
              name: String
            }

            type Query {
              product(id: ID!): Product
            }

            scalar FieldSet

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
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

    // Ruling repo-ctf.24: the same translation applies to an Apollo @authenticated application on
    // a field.
    [Fact]
    public void Transform_AuthenticatedDirective_OnField()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.6"
                import: ["@key", "@authenticated"]
              ) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
              internalNotes: String @authenticated
            }

            type Query {
              product(id: ID!): Product
            }

            scalar FieldSet

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
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

    // Ruling repo-ctf.24: an Apollo @requiresScopes(scopes: [[a, b], [c]]) application translates
    // to TWO cumulative Fusion @policy applications: one requiring fusion.authenticated (a scope
    // check implies authentication) and one carrying the OR-of-AND scope groups, each scope
    // prefixed with fusion.scope:, both with onDenied: ERROR.
    [Fact]
    public void Transform_RequiresScopesDirective_OnField()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.6"
                import: ["@key", "@requiresScopes"]
              ) {
              query: Query
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
              internalNotes: String @requiresScopes(scopes: [["read:internal", "read:audit"], ["admin"]])
            }

            type Query {
              product(id: ID!): Product
            }

            scalar FieldSet
            scalar federation__Scope

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @requiresScopes(scopes: [[federation__Scope!]!]!)
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
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

    // Ruling repo-ctf.24: @authenticated and @requiresScopes must still be recognized and
    // translated when the main federation spec is imported with renamed local directive names,
    // e.g. @link(import: [{name: "@authenticated", as: "@auth"}, ...]). The rewritten
    // applications still carry Fusion's canonical @policy name regardless of the source-schema
    // alias.
    [Fact]
    public void Transform_AuthDirectives_RenamedImport()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.6"
                import: [
                  "@key"
                  { name: "@authenticated", as: "@auth" }
                  { name: "@requiresScopes", as: "@scopes" }
                ]
              ) {
              query: Query
            }

            type Product @key(fields: "id") @auth {
              id: ID!
              name: String @scopes(scopes: [["read:products"]])
            }

            type Query {
              product(id: ID!): Product
            }

            scalar FieldSet
            scalar federation__Scope
            scalar link__Import

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [link__Import]) repeatable on SCHEMA
            directive @auth on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            directive @scopes(scopes: [[federation__Scope!]!]!)
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
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

    // Ruling repo-ctf.24: existing explicit @policy applications stay as separate cumulative
    // applications alongside the translated @authenticated application; they are never flattened
    // into an OR or combined into a cross product.
    [Fact]
    public void Transform_AuthenticatedDirective_StaysCumulativeWithExplicitPolicy()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.6"
                import: ["@key", "@authenticated"]
              )
              @link(url: "https://specs.apollo.dev/policy/v0.1", import: ["@policy"]) {
              query: Query
            }

            type Product @key(fields: "id") @authenticated @policy(policies: [["internal"]]) {
              id: ID!
              name: String
            }

            type Query {
              product(id: ID!): Product
            }

            scalar FieldSet
            scalar federation__Policy

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            directive @policy(policies: [[federation__Policy!]!]!) repeatable
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
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

    // Ruling repo-ctf.24: Fusion's canonical @policy directive only allows the OBJECT and
    // FIELD_DEFINITION locations. An Apollo @authenticated or @requiresScopes application on a
    // scalar or enum type cannot be carried over by the rewrite, so it must fail composition
    // loudly instead of being silently dropped, superseding repo-8fh's silent drop for those
    // locations now that translation exists.
    [Fact]
    public void Transform_AuthDirectives_OnEnumOrScalar()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.6"
                import: ["@key", "@authenticated", "@requiresScopes"]
              ) {
              query: Query
            }

            type Query {
              color: Color
              money: Money
            }

            enum Color @authenticated {
              RED
              BLUE
            }

            scalar Money @requiresScopes(scopes: [["finance"]])

            scalar FieldSet
            scalar federation__Scope

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            directive @requiresScopes(scopes: [[federation__Scope!]!]!)
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.False(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(string.Join(Environment.NewLine, result.Errors.Select(e => e.Message)), "Errors")
            .MatchMarkdownSnapshot();
    }
}

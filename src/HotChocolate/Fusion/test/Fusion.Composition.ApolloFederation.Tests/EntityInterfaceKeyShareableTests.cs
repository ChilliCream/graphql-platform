using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class EntityInterfaceKeyShareableTests
{
    // An Apollo Federation @key may be declared on an interface (entity interface). The key
    // fields on the interface itself must never be stamped @shareable: @shareable is only
    // meaningful on object fields, and the composition's shareable-usage validation rejects it
    // on interfaces. Object key fields, by contrast, must still be stamped so a key field shared
    // across subgraphs composes.
    [Fact]
    public void Compose_Should_Succeed_When_KeyIsDeclaredOnEntityInterface()
    {
        // arrange
        // A single Apollo subgraph whose entity interface carries a @key; before the fix the
        // interface's 'id' field was stamped @shareable and composition failed with
        // INVALID_SHAREABLE_USAGE.
        const string schema =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Query {
              media: [Media!]!
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            interface Media @key(fields: "id") {
              id: ID!
              title: String!
            }

            type Article implements Media @key(fields: "id") {
              id: ID!
              title: String!
            }

            type _Service { sdl: String! }

            union _Entity = Article

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        var composer = new SchemaComposer(
            [new SourceSchemaText("catalog", schema)],
            new SchemaComposerOptions(),
            new CompositionLog());

        // act
        var result = composer.Compose();

        // assert
        Assert.True(
            result.IsSuccess,
            result.IsSuccess ? null : result.Errors[0].Message);
    }

    [Fact]
    public void Compose_Should_Succeed_When_ObjectKeyFieldIsSharedAcrossSubgraphs()
    {
        // arrange
        // Two Apollo subgraphs both contribute the object entity Product and its key field 'id'.
        // This only composes when the object key field is stamped @shareable, so it pins that the
        // interface-only skip did not also drop @shareable on object key fields.
        const string catalog =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Query {
              product: Product
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String!
            }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        const string reviews =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }

            type Query {
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type Product @key(fields: "id") {
              id: ID!
              reviews: [Review!]!
            }

            type Review { body: String! }

            type _Service { sdl: String! }

            union _Entity = Product

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        var composer = new SchemaComposer(
            [new SourceSchemaText("catalog", catalog), new SourceSchemaText("reviews", reviews)],
            new SchemaComposerOptions(),
            new CompositionLog());

        // act
        var result = composer.Compose();

        // assert
        Assert.True(
            result.IsSuccess,
            result.IsSuccess ? null : result.Errors[0].Message);
    }
}

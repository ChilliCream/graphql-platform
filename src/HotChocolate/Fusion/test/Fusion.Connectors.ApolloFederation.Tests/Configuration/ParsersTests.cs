using System.Text.Json;
using HotChocolate.Fusion.Connectors.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Configuration;

public sealed class ParsersTests
{
    [Fact]
    public void TryParse_Should_Claim_When_ConnectorKindIsApollo()
    {
        // arrange
        var schema = ComposeSchema(
            "products",
            """
            schema @fusion__connector(kind: "Apollo") {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var sourceSchema = ReadSourceSchema(
            """
            {
              "products": {
                "transports": { "http": { "url": "http://products/graphql" } }
              }
            }
            """);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        var matched = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.True(matched);
        var federationConfig = Assert.IsType<ApolloFederationSourceSchemaClientConfiguration>(
            Assert.Single(configurations!));
        Assert.Equal("products", federationConfig.Name);
        Assert.Equal("http://products/graphql", federationConfig.BaseAddress.ToString());
    }

    [Fact]
    public void TryParse_Should_NotClaim_When_ConnectorKindIsAbsent()
    {
        // arrange
        var schema = ComposeSchema(
            "catalog",
            """
            schema {
              query: Query
            }

            type Query {
              ping: String
            }
            """);

        var sourceSchema = ReadSourceSchema(
            """
            {
              "catalog": {
                "transports": { "http": { "url": "http://catalog/graphql" } }
              }
            }
            """);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        var matched = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.False(matched);
        Assert.Null(configurations);
    }

    [Fact]
    public void TryParse_Should_NotClaim_When_ConnectorKindIsGraphQL()
    {
        // arrange
        var schema = ComposeSchema(
            "catalog",
            """
            schema @fusion__connector(kind: "GraphQL") {
              query: Query
            }

            type Query {
              ping: String
            }
            """);

        var sourceSchema = ReadSourceSchema(
            """
            {
              "catalog": {
                "transports": { "http": { "url": "http://catalog/graphql" } }
              }
            }
            """);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        var matched = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.False(matched);
        Assert.Null(configurations);
    }

    [Fact]
    public void TryParse_Should_Throw_When_HttpUrlIsMissing()
    {
        // arrange
        var schema = ComposeSchema(
            "products",
            """
            schema @fusion__connector(kind: "Apollo") {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var sourceSchema = ReadSourceSchema(
            """
            {
              "products": {
                "transports": { "http": { } }
              }
            }
            """);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        void Act() => parser.TryParse(schema, sourceSchema, out _);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Contains("transports.http.url", exception.Message);
        Assert.Contains("products", exception.Message);
    }

    [Fact]
    public void TryParse_Should_NotClaim_When_HttpTransportBlockIsMissing()
    {
        // arrange
        var schema = ComposeSchema(
            "products",
            """
            schema @fusion__connector(kind: "Apollo") {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var sourceSchema = ReadSourceSchema(
            """
            {
              "products": {
                "transports": { }
              }
            }
            """);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        var matched = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.False(matched);
        Assert.Null(configurations);
    }

    [Fact]
    public void TryParse_Should_HonorCustomClientName()
    {
        // arrange
        var schema = ComposeSchema(
            "products",
            """
            schema @fusion__connector(kind: "Apollo") {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var sourceSchema = ReadSourceSchema(
            """
            {
              "products": {
                "transports": {
                  "http": {
                    "url": "http://products/graphql",
                    "clientName": "products-client"
                  }
                }
              }
            }
            """);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        var federationConfig = Assert.IsType<ApolloFederationSourceSchemaClientConfiguration>(
            Assert.Single(configurations!));
        Assert.Equal("products-client", federationConfig.HttpClientName);
    }

    private static FusionSchemaDefinition ComposeSchema(string name, string sourceSdl)
    {
        var sources = new[] { new SourceSchemaText(name, sourceSdl) };
        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions();
        var composer = new SchemaComposer(sources, composerOptions, compositionLog);

        var result = composer.Compose();
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }

    private static JsonProperty ReadSourceSchema(string settingsJson)
    {
        var document = JsonDocument.Parse(settingsJson);
        return document.RootElement.EnumerateObject().First();
    }
}

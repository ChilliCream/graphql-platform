using HotChocolate.Fusion.ApolloFederation;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionSchemaDefinitionConnectorKindTests
{
    private const string FederationSubgraphSdl =
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

    [Fact]
    public void SourceSchemaInfo_Should_PreserveThreeValueConstructorAndDeconstruction()
    {
        // arrange
        var sourceSchema = new SourceSchemaInfo("key", "name", "connector");

        // act
        var (key, name, connectorKind) = sourceSchema;

        // assert
        Assert.Equal("key", key);
        Assert.Equal("name", name);
        Assert.Equal("connector", connectorKind);
        Assert.False(sourceSchema.AllowNonResolvableInterfaceObjects);
    }

    [Fact]
    public void GetSourceSchemaConnectorKind_Should_ReturnApolloFederation_When_SourceIsFederation()
    {
        // arrange
        var schema = ComposeSchema(("Products", FederationSubgraphSdl));

        // act
        var kind = schema.GetSourceSchemaConnectorKind("Products");

        // assert
        Assert.Equal("ApolloFederation", kind);
    }

    [Fact]
    public void GetSourceSchemaConnectorKind_Should_ReturnNull_When_SourceIsPlainGraphQL()
    {
        // arrange
        var schema = ComposeSchema(
            ("Catalog",
             """
             type Query {
               ping: String
             }
             """));

        // act
        var kind = schema.GetSourceSchemaConnectorKind("Catalog");

        // assert
        Assert.Null(kind);
    }

    [Fact]
    public void GetSourceSchemaConnectorKind_Should_ReturnNull_When_NameIsNotASourceSchema()
    {
        // arrange
        var schema = ComposeSchema(("Products", FederationSubgraphSdl));

        // act
        var kind = schema.GetSourceSchemaConnectorKind("Unknown");

        // assert
        Assert.Null(kind);
    }

    [Fact]
    public void SourceExternal_Should_DefaultFalse_When_FieldWasNotExternal()
    {
        // arrange
        var schema = ComposeSchema(("Products", FederationSubgraphSdl));

        // act
        var source = schema.Types
            .GetType<FusionObjectTypeDefinition>("Product")
            .Fields["id"]
            .Sources["Products"];

        // assert
        Assert.False(source.IsSourceExternal);
    }

    [Fact]
    public void SourceExternal_Should_RoundTrip_When_ExternalKeyWasPromoted()
    {
        // arrange
        var document = ComposeSchemaDocument(
            ("Products",
             """
             extend schema
               @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

             type Query {
               product: Product
             }

             type Product @key(fields: "id") {
               id: ID!
             }
             """),
            ("Reviews",
             """
             extend schema
               @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external"])

             type Query {
               version: String
             }

             type Product @key(fields: "id") {
               id: ID! @external
               reviews: [String!]
             }
             """));

        // act
        var roundTripped = FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse(document.ToString(indented: true)));
        var source = roundTripped.Types
            .GetType<FusionObjectTypeDefinition>("Product")
            .Fields["id"]
            .Sources["Reviews"];

        // assert
        Assert.False(source.IsExternal);
        Assert.True(source.IsSourceExternal);
    }

    [Fact]
    public void SourceExternal_Should_RoundTrip_When_TransformedSdlIsComposed()
    {
        // arrange
        const string federationSdl =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external"])

            type Query {
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID! @external
            }
            """;

        var transformed = FederationSchemaTransformer.Transform(federationSdl);
        Assert.True(transformed.IsSuccess);

        // act
        var document = ComposeSchemaDocument(("Products", transformed.Value));
        var schema = FusionSchemaDefinition.Create(document);
        var source = schema.Types
            .GetType<FusionObjectTypeDefinition>("Product")
            .Fields["id"]
            .Sources["Products"];

        // assert
        Assert.True(source.IsSourceExternal);
        Assert.Equal(
            0,
            document.Definitions
                .OfType<DirectiveDefinitionNode>()
                .Count(definition => definition.Name.Value == "fusion__sourceExternal"));
        Assert.Equal(
            0,
            document.Definitions
                .OfType<ObjectTypeDefinitionNode>()
                .SelectMany(type => type.Fields)
                .Concat(
                    document.Definitions
                        .OfType<InterfaceTypeDefinitionNode>()
                        .SelectMany(type => type.Fields))
                .Count(field => field.Directives.Any(
                    directive => directive.Name.Value == "fusion__sourceExternal")));
    }

    private static FusionSchemaDefinition ComposeSchema(params (string Name, string Sdl)[] sources)
        => FusionSchemaDefinition.Create(ComposeSchemaDocument(sources));

    private static DocumentNode ComposeSchemaDocument(params (string Name, string Sdl)[] sources)
    {
        var sourceTexts = sources.Select(s => new SourceSchemaText(s.Name, s.Sdl)).ToArray();
        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions();

        foreach (var (name, _) in sources)
        {
            composerOptions.SourceSchemas[name] = new SourceSchemaOptions
            {
                Preprocessor = new SourceSchemaPreprocessorOptions
                {
                    InferKeysFromLookups = false
                }
            };
        }

        var composer = new SchemaComposer(sourceTexts, composerOptions, compositionLog);

        var result = composer.Compose();
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return result.Value.ToSyntaxNode();
    }
}

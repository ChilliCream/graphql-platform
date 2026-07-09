using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;

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

    private static FusionSchemaDefinition ComposeSchema(params (string Name, string Sdl)[] sources)
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

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }
}

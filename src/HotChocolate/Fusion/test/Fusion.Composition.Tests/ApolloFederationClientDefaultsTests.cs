using System.Text.Json;
using HotChocolate.Fusion.ApolloFederation;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Packaging;

namespace HotChocolate.Fusion;

public sealed class ApolloFederationClientDefaultsTests
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
    public void Apply_Should_Disable_Batching_When_Http_Transport_Has_No_Capabilities()
    {
        // arrange
        var settings = JsonDocument.Parse(
            """
            {
                "name": "products",
                "transports": {
                    "http": { "url": "http://localhost:5000/graphql" }
                }
            }
            """);

        // act
        var result = ApolloFederationClientDefaults.Apply(settings);

        // assert
        var batching = GetBatching(result);
        Assert.False(batching.GetProperty("variableBatching").GetBoolean());
        Assert.False(batching.GetProperty("requestBatching").GetBoolean());
    }

    [Fact]
    public void Apply_Should_Preserve_User_Declared_VariableBatching_And_Add_Missing_RequestBatching()
    {
        // arrange
        var settings = JsonDocument.Parse(
            """
            {
                "name": "products",
                "transports": {
                    "http": {
                        "url": "http://localhost:5000/graphql",
                        "capabilities": {
                            "batching": { "variableBatching": true }
                        }
                    }
                }
            }
            """);

        // act
        var result = ApolloFederationClientDefaults.Apply(settings);

        // assert
        var batching = GetBatching(result);
        Assert.True(batching.GetProperty("variableBatching").GetBoolean());
        Assert.False(batching.GetProperty("requestBatching").GetBoolean());
    }

    [Fact]
    public void Apply_Should_Return_Original_When_Both_Batching_Flags_Already_Declared()
    {
        // arrange
        var settings = JsonDocument.Parse(
            """
            {
                "name": "products",
                "transports": {
                    "http": {
                        "url": "http://localhost:5000/graphql",
                        "capabilities": {
                            "batching": { "variableBatching": true, "requestBatching": true }
                        }
                    }
                }
            }
            """);

        // act
        var result = ApolloFederationClientDefaults.Apply(settings);

        // assert
        Assert.Same(settings, result);
    }

    [Fact]
    public void Apply_Should_Return_Original_When_No_Http_Transport_Declared()
    {
        // arrange
        var settings = JsonDocument.Parse(
            """
            {
                "name": "products",
                "transports": {
                    "websockets": { "url": "ws://localhost:5000/graphql" }
                }
            }
            """);

        // act
        var result = ApolloFederationClientDefaults.Apply(settings);

        // assert
        Assert.Same(settings, result);
    }

    [Fact]
    public void AppliesTo_Should_ReturnTrue_When_SourceSchema_Is_ApolloFederation()
    {
        // arrange
        var mergedSchema = Compose(("products", FederationSubgraphSdl));

        // act
        var appliesTo = ApolloFederationClientDefaults.AppliesTo(mergedSchema, "products");

        // assert
        Assert.True(appliesTo);
    }

    [Fact]
    public void AppliesTo_Should_ReturnFalse_When_SourceSchema_Is_PlainGraphQL()
    {
        // arrange
        var mergedSchema = Compose(
            ("catalog",
             """
             type Query {
               ping: String
             }
             """));

        // act
        var appliesTo = ApolloFederationClientDefaults.AppliesTo(mergedSchema, "catalog");

        // assert
        Assert.False(appliesTo);
    }

    [Fact]
    public async Task ComposeAsync_Should_Emit_Conservative_Batching_Default_For_ApolloFederation_Subgraph()
    {
        // arrange
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var settings = JsonDocument.Parse(
            """
            {
                "name": "products",
                "preprocessor": { "inferKeysFromLookups": false },
                "transports": {
                    "http": { "url": "http://localhost:5000/graphql" }
                }
            }
            """);
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>
        {
            ["products"] = (new SourceSchemaText("products", FederationSubgraphSdl), settings)
        };

        // act
        var result = await CompositionHelper.ComposeAsync(
            new CompositionLog(),
            sourceSchemas,
            archive,
            "development",
            compositionSettings: null,
            legacyArchive: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.IsSuccess);
        var gateway = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        var batching = gateway!.Settings.RootElement
            .GetProperty("sourceSchemas")
            .GetProperty("products")
            .GetProperty("transports")
            .GetProperty("http")
            .GetProperty("capabilities")
            .GetProperty("batching");
        Assert.False(batching.GetProperty("variableBatching").GetBoolean());
        Assert.False(batching.GetProperty("requestBatching").GetBoolean());
    }

    private static JsonElement GetBatching(JsonDocument settings)
    {
        return settings.RootElement
            .GetProperty("transports")
            .GetProperty("http")
            .GetProperty("capabilities")
            .GetProperty("batching");
    }

    private static Types.Mutable.MutableSchemaDefinition Compose(params (string Name, string Sdl)[] sources)
    {
        var sourceTexts = sources.Select(s => new SourceSchemaText(s.Name, s.Sdl)).ToArray();
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

        var result = new SchemaComposer(sourceTexts, composerOptions, new CompositionLog()).Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return result.Value;
    }
}

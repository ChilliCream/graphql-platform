using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Features;
using HotChocolate.Fusion.Configuration.Parsers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

public class DefaultGraphQLClientConfigurationParserTests : FusionTestBase
{
    [Fact]
    public void DefaultGraphQLClientConfigurationParser_Should_Return_False_When_Http_Transport_Missing()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var sourceSchema = GetSourceSchemaProperty(
            """
            {
                "sourceSchemas": {
                    "a": {
                        "transports": {
                            "websockets": { "url": "ws://localhost:5000/graphql" }
                        }
                    }
                }
            }
            """,
            "a");
        var parser = new DefaultGraphQLClientConfigurationParser();

        // act
        var claimed = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.False(claimed);
        Assert.Null(configurations);
    }

    [Fact]
    public void DefaultGraphQLClientConfigurationParser_Should_Produce_Configuration_When_Http_Transport_Present()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var sourceSchema = GetSourceSchemaProperty(
            """
            {
                "sourceSchemas": {
                    "products": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "clientName": "products-client"
                            }
                        }
                    }
                }
            }
            """,
            "products");
        var parser = new DefaultGraphQLClientConfigurationParser();

        // act
        var claimed = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(Assert.Single(configurations!));
        Summarize(http).MatchInlineSnapshot(
            """
            Name: products
            HttpClientName: products-client
            BaseAddress: http://localhost:5000/graphql
            SupportedOperations: All
            Capabilities: All
            OnError: <null>
            """);
    }

    [Fact]
    public void DefaultGraphQLClientConfigurationParser_Should_Disable_VariableBatching_Capability_When_Configured()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var sourceSchema = GetSourceSchemaProperty(
            """
            {
                "sourceSchemas": {
                    "products": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "capabilities": {
                                    "batching": {
                                        "variableBatching": false
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """,
            "products");
        var parser = new DefaultGraphQLClientConfigurationParser();

        // act
        var claimed = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(Assert.Single(configurations!));
        Assert.Equal(SourceSchemaClientCapabilities.RequestBatching, http.Capabilities);
    }

    [Fact]
    public void DefaultGraphQLClientConfigurationParser_Should_Honor_Declared_VariableBatching_When_Schema_Is_ApolloFederation()
    {
        // arrange
        // the source schema is composed as an Apollo Federation connector, yet the
        // kind-blind parser honors the batching capability declared in its settings.
        var schema = ComposeApolloFederationSchema("products");
        Assert.Equal("ApolloFederation", schema.GetSourceSchemaConnectorKind("products"));
        var sourceSchema = GetSourceSchemaProperty(
            """
            {
                "sourceSchemas": {
                    "products": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "capabilities": {
                                    "batching": {
                                        "variableBatching": true
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """,
            "products");
        var parser = new DefaultGraphQLClientConfigurationParser();

        // act
        var claimed = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(Assert.Single(configurations!));
        Assert.Equal(SourceSchemaClientCapabilities.All, http.Capabilities);
    }

    [Fact]
    public void DefaultGraphQLClientConfigurationParser_Should_Disable_Subscription_Operations_When_Configured()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var sourceSchema = GetSourceSchemaProperty(
            """
            {
                "sourceSchemas": {
                    "products": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "capabilities": {
                                    "subscriptions": {
                                        "supported": false
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """,
            "products");
        var parser = new DefaultGraphQLClientConfigurationParser();

        // act
        var claimed = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(Assert.Single(configurations!));
        Assert.Equal(
            SupportedOperationType.Query | SupportedOperationType.Mutation,
            http.SupportedOperations);
    }

    [Fact]
    public void DefaultGraphQLClientConfigurationParser_Should_Set_OnError_To_Null_Mode_When_Configured_As_NULL()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var sourceSchema = GetSourceSchemaProperty(
            """
            {
                "sourceSchemas": {
                    "products": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "capabilities": {
                                    "onError": "NULL"
                                }
                            }
                        }
                    }
                }
            }
            """,
            "products");
        var parser = new DefaultGraphQLClientConfigurationParser();

        // act
        var claimed = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(Assert.Single(configurations!));
        Assert.Equal(ErrorHandlingMode.Null, http.OnError);
    }

    [Fact]
    public void DefaultGraphQLClientConfigurationParser_Should_Set_OnError_To_Propagate_When_Configured_As_PROPAGATE()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var sourceSchema = GetSourceSchemaProperty(
            """
            {
                "sourceSchemas": {
                    "products": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "capabilities": {
                                    "onError": "PROPAGATE"
                                }
                            }
                        }
                    }
                }
            }
            """,
            "products");
        var parser = new DefaultGraphQLClientConfigurationParser();

        // act
        var claimed = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(Assert.Single(configurations!));
        Assert.Equal(ErrorHandlingMode.Propagate, http.OnError);
    }

    [Fact]
    public void DefaultGraphQLClientConfigurationParser_Should_Leave_OnError_Null_When_Property_Missing()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var sourceSchema = GetSourceSchemaProperty(
            """
            {
                "sourceSchemas": {
                    "products": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "capabilities": {}
                            }
                        }
                    }
                }
            }
            """,
            "products");
        var parser = new DefaultGraphQLClientConfigurationParser();

        // act
        var claimed = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(Assert.Single(configurations!));
        Assert.Null(http.OnError);
    }

    [Fact]
    public void DefaultGraphQLClientConfigurationParser_Should_Leave_OnError_Null_When_Value_Is_Json_Null()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var sourceSchema = GetSourceSchemaProperty(
            """
            {
                "sourceSchemas": {
                    "products": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "capabilities": {
                                    "onError": null
                                }
                            }
                        }
                    }
                }
            }
            """,
            "products");
        var parser = new DefaultGraphQLClientConfigurationParser();

        // act
        var claimed = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(Assert.Single(configurations!));
        Assert.Null(http.OnError);
    }

    [Fact]
    public void DefaultGraphQLClientConfigurationParser_Should_Leave_OnError_Null_When_Value_Is_Unknown_String()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var sourceSchema = GetSourceSchemaProperty(
            """
            {
                "sourceSchemas": {
                    "products": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql",
                                "capabilities": {
                                    "onError": "BOGUS"
                                }
                            }
                        }
                    }
                }
            }
            """,
            "products");
        var parser = new DefaultGraphQLClientConfigurationParser();

        // act
        var claimed = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(Assert.Single(configurations!));
        Assert.Null(http.OnError);
    }

    [Fact]
    public void DefaultGraphQLClientConfigurationParser_Should_Default_ClientName_When_Not_Provided()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var sourceSchema = GetSourceSchemaProperty(
            """
            {
                "sourceSchemas": {
                    "products": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql"
                            }
                        }
                    }
                }
            }
            """,
            "products");
        var parser = new DefaultGraphQLClientConfigurationParser();

        // act
        var claimed = parser.TryParse(schema, sourceSchema, out var configurations);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(Assert.Single(configurations!));
        Assert.Equal(HttpSourceSchemaClientConfiguration.DefaultClientName, http.HttpClientName);
    }

    [Fact]
    public async Task CreateClientConfigurations_Should_Throw_When_No_Parser_Claims_Schema()
    {
        // arrange
        var config = CreateConfigurationWithSettings(
            """
            {
                "sourceSchemas": {
                    "a": {
                        "transports": {
                            "xyz": { "url": "xyz://localhost" }
                        }
                    }
                }
            }
            """);

        var configProvider = new TestFusionConfigurationProvider(config);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        // act
        async Task Act() => await manager.GetExecutorAsync();

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Act);
        Assert.Equal(
            "The source schema configuration of 'a' could not be parsed and no client "
            + "configuration was registered for it in code.",
            exception.Message);
    }

    [Fact]
    public async Task CreateClientConfigurations_Should_Not_Throw_When_Modifier_Provides_Missing_Configuration()
    {
        // arrange
        // settings carry only a non-http transport, but a client configuration for "a"
        // is supplied in code via AddHttpClientConfiguration.
        var config = CreateConfigurationWithSettings(
            """
            {
                "sourceSchemas": {
                    "a": {
                        "transports": {
                            "xyz": { "url": "xyz://localhost" }
                        }
                    }
                }
            }
            """);

        var configProvider = new TestFusionConfigurationProvider(config);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .AddHttpClientConfiguration(
                    new HttpSourceSchemaClientConfiguration(
                        name: "a",
                        httpClientName: HttpSourceSchemaClientConfiguration.DefaultClientName,
                        baseAddress: new Uri("http://localhost:5000/graphql")))
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        // act
        var executor = await manager.GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var clientConfigs = executor.Schema.Features.GetRequired<SourceSchemaClientConfigurations>();
        Assert.True(clientConfigs.TryGet("a", OperationType.Query, out var queryConfig));
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(queryConfig);
        Assert.Equal(new Uri("http://localhost:5000/graphql"), http.BaseAddress);
    }

    [Fact]
    public async Task CreateClientConfigurations_Should_Prefer_User_Parser_Over_Builtin()
    {
        // arrange
        var config = CreateConfigurationWithSettings(
            """
            {
                "sourceSchemas": {
                    "a": {
                        "transports": {
                            "http": {
                                "url": "http://localhost:5000/graphql"
                            }
                        }
                    }
                }
            }
            """);

        var configProvider = new TestFusionConfigurationProvider(config);
        var userParser = new AlwaysClaimParser();

        var builder =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider);

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.SourceSchemaClientConfigurationParsers.Add(userParser));

        var services = builder.Services.BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();

        // act
        var executor = await manager.GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var clientConfigs = executor.Schema.Features.GetRequired<SourceSchemaClientConfigurations>();
        Assert.True(clientConfigs.TryGet("a", OperationType.Query, out var queryConfig));
        Assert.IsType<StubClientConfiguration>(queryConfig);
    }

    private static FusionSchemaDefinition ComposeApolloFederationSchema(string name)
    {
        var sdl =
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

        var composerOptions = new SchemaComposerOptions();
        composerOptions.SourceSchemas[name] = new SourceSchemaOptions
        {
            Preprocessor = new SourceSchemaPreprocessorOptions
            {
                InferKeysFromLookups = false
            }
        };

        var result = new SchemaComposer(
            [new SourceSchemaText(name, sdl)],
            composerOptions,
            new CompositionLog()).Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }

    private static JsonProperty GetSourceSchemaProperty(string settingsJson, string schemaName)
    {
        var document = JsonDocument.Parse(settingsJson);
        var sourceSchemas = document.RootElement.GetProperty("sourceSchemas");

        foreach (var candidate in sourceSchemas.EnumerateObject())
        {
            if (candidate.Name == schemaName)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"Source schema '{schemaName}' not found.");
    }

    private static string Summarize(HttpSourceSchemaClientConfiguration configuration)
    {
        return $"""
            Name: {configuration.Name}
            HttpClientName: {configuration.HttpClientName}
            BaseAddress: {configuration.BaseAddress}
            SupportedOperations: {configuration.SupportedOperations}
            Capabilities: {configuration.Capabilities}
            OnError: {configuration.OnError?.ToString() ?? "<null>"}
            """;
    }

    private static FusionConfiguration CreateConfigurationWithSettings(string settingsJson)
    {
        var compositeSchema = ComposeSchemaDocument("type Query { foo: String }");
        var settings = JsonDocument.Parse(settingsJson);

        return new FusionConfiguration(
            compositeSchema,
            new JsonDocumentOwner(settings));
    }

    private sealed class AlwaysClaimParser : ISourceSchemaClientConfigurationParser
    {
        public bool TryParse(
            FusionSchemaDefinition schema,
            JsonProperty sourceSchema,
            [NotNullWhen(true)] out ISourceSchemaClientConfiguration[]? configurations)
        {
            configurations = [new StubClientConfiguration(sourceSchema.Name)];
            return true;
        }
    }

    private sealed class StubClientConfiguration(string name) : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.All;
    }
}

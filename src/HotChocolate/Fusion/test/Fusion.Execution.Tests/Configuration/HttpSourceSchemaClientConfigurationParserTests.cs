using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Features;
using HotChocolate.Fusion.Configuration.Parsers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

public class HttpSourceSchemaClientConfigurationParserTests : FusionTestBase
{
    [Fact]
    public void HttpSourceSchemaClientConfigurationParser_Should_Return_False_When_Http_Transport_Missing()
    {
        // arrange
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
        var transport = GetTransportProperty(sourceSchema, "websockets");
        var parser = new HttpSourceSchemaClientConfigurationParser();

        // act
        var claimed = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.False(claimed);
        Assert.Null(configuration);
    }

    [Fact]
    public void HttpSourceSchemaClientConfigurationParser_Should_Produce_Configuration_When_Http_Transport_Present()
    {
        // arrange
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
        var transport = GetTransportProperty(sourceSchema, "http");
        var parser = new HttpSourceSchemaClientConfigurationParser();

        // act
        var claimed = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(configuration);
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
    public void HttpSourceSchemaClientConfigurationParser_Should_Disable_VariableBatching_Capability_When_Configured()
    {
        // arrange
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
        var transport = GetTransportProperty(sourceSchema, "http");
        var parser = new HttpSourceSchemaClientConfigurationParser();

        // act
        var claimed = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(configuration);
        Assert.Equal(SourceSchemaClientCapabilities.RequestBatching, http.Capabilities);
    }

    [Fact]
    public void HttpSourceSchemaClientConfigurationParser_Should_Disable_Subscription_Operations_When_Configured()
    {
        // arrange
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
        var transport = GetTransportProperty(sourceSchema, "http");
        var parser = new HttpSourceSchemaClientConfigurationParser();

        // act
        var claimed = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(configuration);
        Assert.Equal(
            SupportedOperationType.Query | SupportedOperationType.Mutation,
            http.SupportedOperations);
    }

    [Fact]
    public void HttpSourceSchemaClientConfigurationParser_Should_Set_OnError_To_Null_Mode_When_Configured_As_NULL()
    {
        // arrange
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
        var transport = GetTransportProperty(sourceSchema, "http");
        var parser = new HttpSourceSchemaClientConfigurationParser();

        // act
        var claimed = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(configuration);
        Assert.Equal(ErrorHandlingMode.Null, http.OnError);
    }

    [Fact]
    public void HttpSourceSchemaClientConfigurationParser_Should_Set_OnError_To_Propagate_When_Configured_As_PROPAGATE()
    {
        // arrange
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
        var transport = GetTransportProperty(sourceSchema, "http");
        var parser = new HttpSourceSchemaClientConfigurationParser();

        // act
        var claimed = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(configuration);
        Assert.Equal(ErrorHandlingMode.Propagate, http.OnError);
    }

    [Fact]
    public void HttpSourceSchemaClientConfigurationParser_Should_Leave_OnError_Null_When_Property_Missing()
    {
        // arrange
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
        var transport = GetTransportProperty(sourceSchema, "http");
        var parser = new HttpSourceSchemaClientConfigurationParser();

        // act
        var claimed = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(configuration);
        Assert.Null(http.OnError);
    }

    [Fact]
    public void HttpSourceSchemaClientConfigurationParser_Should_Leave_OnError_Null_When_Value_Is_Json_Null()
    {
        // arrange
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
        var transport = GetTransportProperty(sourceSchema, "http");
        var parser = new HttpSourceSchemaClientConfigurationParser();

        // act
        var claimed = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(configuration);
        Assert.Null(http.OnError);
    }

    [Fact]
    public void HttpSourceSchemaClientConfigurationParser_Should_Leave_OnError_Null_When_Value_Is_Unknown_String()
    {
        // arrange
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
        var transport = GetTransportProperty(sourceSchema, "http");
        var parser = new HttpSourceSchemaClientConfigurationParser();

        // act
        var claimed = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(configuration);
        Assert.Null(http.OnError);
    }

    [Fact]
    public void HttpSourceSchemaClientConfigurationParser_Should_Default_ClientName_When_Not_Provided()
    {
        // arrange
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
        var transport = GetTransportProperty(sourceSchema, "http");
        var parser = new HttpSourceSchemaClientConfigurationParser();

        // act
        var claimed = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(claimed);
        var http = Assert.IsType<HttpSourceSchemaClientConfiguration>(configuration);
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
        Assert.Equal("No parser claimed any transport for source schema 'a'.", exception.Message);
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
        var executor = await manager.GetExecutorAsync();

        // assert
        var clientConfigs = executor.Schema.Features.GetRequired<SourceSchemaClientConfigurations>();
        Assert.True(clientConfigs.TryGet("a", OperationType.Query, out var queryConfig));
        Assert.IsType<StubClientConfiguration>(queryConfig);
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

    private static JsonProperty GetTransportProperty(JsonProperty sourceSchema, string transportName)
    {
        var transports = sourceSchema.Value.GetProperty("transports");

        foreach (var candidate in transports.EnumerateObject())
        {
            if (candidate.Name == transportName)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"Transport '{transportName}' not found.");
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
            JsonProperty sourceSchema,
            JsonProperty transport,
            [NotNullWhen(true)] out ISourceSchemaClientConfiguration? configuration)
        {
            configuration = new StubClientConfiguration(sourceSchema.Name);
            return true;
        }
    }

    private sealed class StubClientConfiguration(string name) : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.All;
    }
}

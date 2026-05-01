using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

public class ParsersTests : FusionTestBase
{
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
        Assert.Equal("No parser claimed source schema 'a'.", exception.Message);
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

    [Fact]
    public async Task DefaultParser_Should_Throw_When_LegacyApolloFederationBlockIsPresent()
    {
        // arrange
        // The settings JSON carries the legacy v15 'extensions.apolloFederation'
        // block, but the schema does not declare '@fusion__connector(kind: "Apollo")'.
        // The default parser must fail loudly at executor build time so users
        // discover the v16 migration mismatch instead of silently dropping
        // federation metadata.
        var config = CreateConfigurationWithSettings(
            """
            {
                "sourceSchemas": {
                    "a": {
                        "transports": {
                            "http": { "url": "http://localhost:5000/graphql" }
                        },
                        "extensions": {
                            "apolloFederation": {
                                "lookups": { "ping": { "entityType": "Foo" } }
                            }
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
        Assert.Contains("legacy 'extensions.apolloFederation'", exception.Message);
        Assert.Contains("'a'", exception.Message);
        Assert.Contains("FederationSchemaTransformer", exception.Message);
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

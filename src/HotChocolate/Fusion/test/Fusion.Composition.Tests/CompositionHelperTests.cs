using System.Text.Json;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;

namespace HotChocolate.Fusion;

public sealed class CompositionHelperTests
{
    [Fact]
    public async Task ComposeAsync_Should_NotTransferCarriedSettingsOwnership_When_ArchiveHasExistingSchema()
    {
        // arrange
        using var productsSettings = JsonDocument.Parse("""{ "name": "Products" }""");
        var products = new Dictionary<string, (SourceSchemaText, JsonDocument)>
        {
            ["Products"] =
            (
                new SourceSchemaText("Products", "type Query { product: String }"),
                productsSettings)
        };
        var stream = new MemoryStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            var result = await CompositionHelper.ComposeAsync(
                new CompositionLog(),
                products,
                archive,
                "Development",
                compositionSettings: null,
                legacyArchive: null,
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
        }

        stream.Position = 0;
        using var reviewsSettings = JsonDocument.Parse("""{ "name": "Reviews" }""");
        var reviews = new Dictionary<string, (SourceSchemaText, JsonDocument)>
        {
            ["Reviews"] =
            (
                new SourceSchemaText("Reviews", "type Query { review: String }"),
                reviewsSettings)
        };

        // act
        using (var archive = FusionArchive.Open(
            stream,
            FusionArchiveMode.Update,
            leaveOpen: true))
        {
            var result = await CompositionHelper.ComposeAsync(
                new CompositionLog(),
                reviews,
                archive,
                "Development",
                compositionSettings: null,
                legacyArchive: null,
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
        }

        // assert
        Assert.Equal(["Reviews"], reviews.Keys);

        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        Assert.Equal(
            ["Products", "Reviews"],
            await readArchive.GetSourceSchemaNamesAsync(
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ComposeAsync_Should_UseStandardParserAndPreserveMarker_When_Version2SupportIsConfigured()
    {
        // arrange
        using var sourceSettings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "2.0"
                  }
                }
              }
            }
            """);
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>
        {
            ["Products"] =
            (
                new SourceSchemaText(
                    "Products",
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
                sourceSettings)
        };
        var stream = new MemoryStream();
        var log = new CompositionLog();

        // act
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            var result = await CompositionHelper.ComposeAsync(
                log,
                sourceSchemas,
                archive,
                "Development",
                compositionSettings: null,
                legacyArchive: null,
                TestContext.Current.CancellationToken);

            Assert.True(
                result.IsSuccess,
                string.Join(Environment.NewLine, log.Select(entry => entry.Message)));
        }

        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        using var sourceConfiguration = Assert.IsType<SourceSchemaConfiguration>(
            await readArchive.TryGetSourceSchemaConfigurationAsync(
                "Products",
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "2.0",
            sourceConfiguration.Settings.RootElement
                .GetProperty("extensions")
                .GetProperty("chillicream")
                .GetProperty("apolloFederationSupport")
                .GetProperty("version")
                .GetString());
    }

    [Fact]
    public async Task ComposeAsync_Should_NotUseLegacyParser_When_Version2SupportIsConfigured()
    {
        // arrange
        using var sourceSettings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "2.0"
                  }
                }
              }
            }
            """);
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>
        {
            ["Products"] =
            (
                new SourceSchemaText(
                    "Products",
                    """
                    scalar _Any
                    scalar _FieldSet

                    type _Service {
                      sdl: String
                    }

                    union _Entity = Product

                    type Query {
                      product: Product
                      _entities(representations: [_Any!]!): [_Entity]!
                      _service: _Service!
                    }

                    type Product @key(fields: "id") @extends {
                      id: ID! @external
                    }
                    """),
                sourceSettings)
        };
        var stream = new MemoryStream();
        var log = new CompositionLog();

        // act
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var result = await CompositionHelper.ComposeAsync(
            log,
            sourceSchemas,
            archive,
            "Development",
            compositionSettings: null,
            legacyArchive: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ComposeAsync_Should_RetainSourceExtensionAndRemoveRuntimeExtension_When_V1SupportIsConsumed()
    {
        // arrange
        using var sourceSettings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "preprocessor": {
                "inferKeysFromLookups": false
              },
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "1.0"
                  },
                  "sibling": {
                    "enabled": true
                  }
                },
                "vendor": {
                  "mode": "test"
                }
              }
            }
            """);
        var sourceSchemas = new Dictionary<string, (SourceSchemaText, JsonDocument)>
        {
            ["Products"] =
            (
                new SourceSchemaText(
                    "Products",
                    """
                    scalar _Any
                    scalar _FieldSet

                    type _Service {
                      sdl: String
                    }

                    union _Entity = Product

                    type Query {
                      product: Product
                      _entities(representations: [_Any!]!): [_Entity]!
                      _service: _Service!
                    }

                    type Product @key(fields: "id") {
                      id: ID!
                    }
                    """),
                sourceSettings)
        };
        var stream = new MemoryStream();
        var log = new CompositionLog();

        // act
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            var result = await CompositionHelper.ComposeAsync(
                log,
                sourceSchemas,
                archive,
                "Development",
                compositionSettings: null,
                legacyArchive: null,
                TestContext.Current.CancellationToken);

            Assert.True(
                result.IsSuccess,
                string.Join(Environment.NewLine, log.Select(entry => entry.Message)));
        }

        stream.Position = 0;

        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        using var sourceConfiguration = Assert.IsType<SourceSchemaConfiguration>(
            await readArchive.TryGetSourceSchemaConfigurationAsync(
                "Products",
                TestContext.Current.CancellationToken));
        using var gatewayConfiguration = Assert.IsType<GatewayConfiguration>(
            await readArchive.TryGetGatewayConfigurationAsync(
                WellKnownVersions.LatestGatewayFormatVersion,
                TestContext.Current.CancellationToken));

        // assert
        var snapshot = new
        {
            ArchivedSourceSettings = sourceConfiguration.Settings.RootElement,
            RuntimeGatewaySettings = gatewayConfiguration.Settings.RootElement
        };

        JsonSerializer.Serialize(
            snapshot,
            new JsonSerializerOptions { WriteIndented = true }).MatchInlineSnapshot(
            """
            {
              "ArchivedSourceSettings": {
                "name": "Products",
                "preprocessor": {
                  "inferKeysFromLookups": false
                },
                "extensions": {
                  "chillicream": {
                    "apolloFederationSupport": {
                      "version": "1.0"
                    },
                    "sibling": {
                      "enabled": true
                    }
                  },
                  "vendor": {
                    "mode": "test"
                  }
                }
              },
              "RuntimeGatewaySettings": {
                "sourceSchemas": {
                  "Products": {
                    "preprocessor": {
                      "inferKeysFromLookups": false
                    },
                    "extensions": {
                      "chillicream": {
                        "sibling": {
                          "enabled": true
                        }
                      },
                      "vendor": {
                        "mode": "test"
                      }
                    }
                  }
                }
              }
            }
            """);
    }
}

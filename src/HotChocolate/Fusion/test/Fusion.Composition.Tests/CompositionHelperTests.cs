using System.Text.Json;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;

namespace HotChocolate.Fusion;

public sealed class CompositionHelperTests
{
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

using System.Text.Json;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

public sealed class CompositionHelperTests
{
    [Fact]
    public async Task ComposeAsync_Should_NotTransferCarriedSettingsOwnership_When_ArchiveHasExistingSchema()
    {
        // arrange
        using var productsSettings = JsonDocument.Parse("""{ "name": "Products" }""");
        var products = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
                new SourceSchemaText("Products", "type Query { product: String }"),
                productsSettings,
                urlOverride: null)
        };
        var stream = new MemoryStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            var result = await CompositionHelper.ComposeAsync(
                new CompositionLog(),
                products,
                archive,
                "Development",
                preferDevUrls: false,
                compositionSettings: null,
                legacyArchive: null,
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
        }

        stream.Position = 0;
        using var reviewsSettings = JsonDocument.Parse("""{ "name": "Reviews" }""");
        var reviews = new Dictionary<string, LocalSourceSchema>
        {
            ["Reviews"] = new(
                new SourceSchemaText("Reviews", "type Query { review: String }"),
                reviewsSettings,
                urlOverride: null)
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
                preferDevUrls: false,
                compositionSettings: null,
                legacyArchive: null,
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
        }

        // assert
        Assert.Equal(["Reviews"], reviews.Keys);
        Assert.Equal(
            "Reviews",
            reviewsSettings.RootElement.GetProperty("name").GetString());

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
        var sourceSchemas = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
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
                sourceSettings,
                urlOverride: null)
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
                preferDevUrls: false,
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
        var sourceSchemas = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
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
                sourceSettings,
                urlOverride: null)
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
            preferDevUrls: false,
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
        var sourceSchemas = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
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
                sourceSettings,
                urlOverride: null)
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
                preferDevUrls: false,
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

    [Fact]
    public async Task ComposeAsync_Should_ComposeLocalUrl_When_LocalSchemaCarriesUrlOverride()
    {
        // arrange
        using var sourceSettings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "https://products.internal.example.com/graphql",
                  "devUrl": "https://products.dev.example.com/graphql"
                }
              }
            }
            """);
        var sourceSchemas = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
                new SourceSchemaText("Products", "type Query { product: String }"),
                sourceSettings,
                urlOverride: new Uri("http://localhost:5001/graphql"))
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
                preferDevUrls: true,
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
                "transports": {
                  "http": {
                    "url": "https://products.internal.example.com/graphql",
                    "devUrl": "https://products.dev.example.com/graphql"
                  }
                }
              },
              "RuntimeGatewaySettings": {
                "sourceSchemas": {
                  "Products": {
                    "transports": {
                      "http": {
                        "url": "http://localhost:5001/graphql"
                      }
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ComposeAsync_Should_EmitCostOptions_When_DefaultListSizeSettingIsSet()
    {
        // arrange
        using var productsSettings = JsonDocument.Parse("""{ "name": "Products" }""");
        var sourceSchemas = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
                new SourceSchemaText("Products", "type Query { product: String }"),
                productsSettings,
                urlOverride: null)
        };
        var stream = new MemoryStream();
        var log = new CompositionLog();
        var compositionSettings = new CompositionSettings
        {
            Merger = { DefaultListSize = 7 }
        };
        using var archive = FusionArchive.Create(stream, leaveOpen: true);

        // act
        var result = await CompositionHelper.ComposeAsync(
            log,
            sourceSchemas,
            archive,
            "Development",
            preferDevUrls: false,
            compositionSettings,
            legacyArchive: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(
            result.IsSuccess,
            string.Join(Environment.NewLine, log.Select(entry => entry.Message)));
        var document = result.Value.ToSyntaxNode();
        var schemaDefinition = document.Definitions.OfType<SchemaDefinitionNode>().Single();
        var costOptionsApplication = Assert.Single(
            schemaDefinition.Directives,
            directive => directive.Name.Value == "fusion__cost_options");

        costOptionsApplication.ToString().MatchInlineSnapshot(
            """
            @fusion__cost_options(defaultListSize: 7)
            """);
    }

    [Fact]
    public async Task ComposeAsync_Should_ReportCompositionError_When_DefaultListSizeSettingIsNegative()
    {
        // arrange
        using var productsSettings = JsonDocument.Parse("""{ "name": "Products" }""");
        var sourceSchemas = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
                new SourceSchemaText("Products", "type Query { product: String }"),
                productsSettings,
                urlOverride: null)
        };
        var stream = new MemoryStream();
        var log = new CompositionLog();
        var compositionSettings = new CompositionSettings
        {
            Merger = { DefaultListSize = -1 }
        };
        using var archive = FusionArchive.Create(stream, leaveOpen: true);

        // act
        var result = await CompositionHelper.ComposeAsync(
            log,
            sourceSchemas,
            archive,
            "Development",
            preferDevUrls: false,
            compositionSettings,
            legacyArchive: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log, e => e.Code == LogEntryCodes.InvalidDefaultListSizeSetting);
        Assert.Equal(
            "The 'defaultListSize' composition setting must be a non-negative integer "
            + "no larger than 2147483647 (-1).",
            entry.Message);
    }

    [Fact]
    public async Task ComposeAsync_Should_EmitCostOptions_When_ArchiveSettingsJsonHasValidDefaultListSize()
    {
        // arrange
        using var productsSettings = JsonDocument.Parse("""{ "name": "Products" }""");
        var sourceSchemas = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
                new SourceSchemaText("Products", "type Query { product: String }"),
                productsSettings,
                urlOverride: null)
        };
        var stream = new MemoryStream();
        var log = new CompositionLog();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        using (var rawCompositionSettings = JsonDocument.Parse("""{ "merger": { "defaultListSize": 7 } }"""))
        {
            await archive.SetCompositionSettingsAsync(
                rawCompositionSettings,
                TestContext.Current.CancellationToken);
        }

        // act
        var result = await CompositionHelper.ComposeAsync(
            log,
            sourceSchemas,
            archive,
            "Development",
            preferDevUrls: false,
            compositionSettings: null,
            legacyArchive: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(
            result.IsSuccess,
            string.Join(Environment.NewLine, log.Select(entry => entry.Message)));
        var document = result.Value.ToSyntaxNode();
        var schemaDefinition = document.Definitions.OfType<SchemaDefinitionNode>().Single();
        var costOptionsApplication = Assert.Single(
            schemaDefinition.Directives,
            directive => directive.Name.Value == "fusion__cost_options");

        costOptionsApplication.ToString().MatchInlineSnapshot(
            """
            @fusion__cost_options(defaultListSize: 7)
            """);
    }

    [Fact]
    public async Task ComposeAsync_Should_ReportCompositionError_When_ArchiveSettingsJsonHasNegativeDefaultListSize()
    {
        // arrange
        using var productsSettings = JsonDocument.Parse("""{ "name": "Products" }""");
        var sourceSchemas = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
                new SourceSchemaText("Products", "type Query { product: String }"),
                productsSettings,
                urlOverride: null)
        };
        var stream = new MemoryStream();
        var log = new CompositionLog();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        using (var rawCompositionSettings = JsonDocument.Parse("""{ "merger": { "defaultListSize": -1 } }"""))
        {
            await archive.SetCompositionSettingsAsync(
                rawCompositionSettings,
                TestContext.Current.CancellationToken);
        }

        // act
        var result = await CompositionHelper.ComposeAsync(
            log,
            sourceSchemas,
            archive,
            "Development",
            preferDevUrls: false,
            compositionSettings: null,
            legacyArchive: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log, e => e.Code == LogEntryCodes.InvalidDefaultListSizeSetting);
        Assert.Equal(
            "The 'defaultListSize' composition setting must be a non-negative integer "
            + "no larger than 2147483647 (-1).",
            entry.Message);
    }

    [Theory]
    [InlineData("""{ "merger": { "defaultListSize": "abc" } }""", "\"abc\"")]
    [InlineData("""{ "merger": { "defaultListSize": 1.5 } }""", "1.5")]
    [InlineData("""{ "merger": { "defaultListSize": 1.0 } }""", "1.0")]
    public async Task ComposeAsync_Should_ReportCompositionError_When_ArchiveSettingsJsonHasNonIntegerDefaultListSize(
        string rawCompositionSettingsJson,
        string expectedRawValue)
    {
        // arrange
        using var productsSettings = JsonDocument.Parse("""{ "name": "Products" }""");
        var sourceSchemas = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
                new SourceSchemaText("Products", "type Query { product: String }"),
                productsSettings,
                urlOverride: null)
        };
        var stream = new MemoryStream();
        var log = new CompositionLog();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        using (var rawCompositionSettings = JsonDocument.Parse(rawCompositionSettingsJson))
        {
            await archive.SetCompositionSettingsAsync(
                rawCompositionSettings,
                TestContext.Current.CancellationToken);
        }

        // act
        var result = await CompositionHelper.ComposeAsync(
            log,
            sourceSchemas,
            archive,
            "Development",
            preferDevUrls: false,
            compositionSettings: null,
            legacyArchive: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log, e => e.Code == LogEntryCodes.InvalidDefaultListSizeSetting);
        Assert.Equal(
            $"The 'defaultListSize' composition setting must be an integer ({expectedRawValue}).",
            entry.Message);
    }

    [Theory]
    [InlineData("9999999999")]
    [InlineData("18446744073709551616")]
    [InlineData("1e1")]
    public async Task ComposeAsync_Should_ReportCompositionError_When_ArchiveSettingsJsonHasOutOfRangeDefaultListSize(
        string rawValue)
    {
        // arrange
        using var productsSettings = JsonDocument.Parse("""{ "name": "Products" }""");
        var sourceSchemas = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
                new SourceSchemaText("Products", "type Query { product: String }"),
                productsSettings,
                urlOverride: null)
        };
        var stream = new MemoryStream();
        var log = new CompositionLog();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        using (var rawCompositionSettings =
            JsonDocument.Parse($$"""{ "merger": { "defaultListSize": {{rawValue}} } }"""))
        {
            await archive.SetCompositionSettingsAsync(
                rawCompositionSettings,
                TestContext.Current.CancellationToken);
        }

        // act
        var result = await CompositionHelper.ComposeAsync(
            log,
            sourceSchemas,
            archive,
            "Development",
            preferDevUrls: false,
            compositionSettings: null,
            legacyArchive: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log, e => e.Code == LogEntryCodes.InvalidDefaultListSizeSetting);
        Assert.Equal(
            "The 'defaultListSize' composition setting must be a non-negative integer "
            + $"no larger than 2147483647 ({rawValue}).",
            entry.Message);
    }
}

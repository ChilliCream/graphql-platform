using System.Text.Json;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Packaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotChocolate.Fusion.Aspire;

public sealed class AspireCompositionHelperTests
{
    private const string ProductsSchemaText = "type Query { product: String }";

    [Theory]
    [InlineData(null)]
    [InlineData(NodeResolution.Gateway)]
    [InlineData(NodeResolution.SourceSchema)]
    public void CreateCompositionSettings_Should_MapNodeResolution(
        NodeResolution? nodeResolution)
    {
        var settings = new GraphQLCompositionSettings
        {
            EnableGlobalObjectIdentification = true,
            NodeResolution = nodeResolution
        };

        var compositionSettings = AspireCompositionHelper.CreateCompositionSettings(settings);

        Assert.True(compositionSettings.Merger.EnableGlobalObjectIdentification);
        Assert.Equal(nodeResolution, compositionSettings.Merger.NodeResolution);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ShareableFieldRuntimeTypeRouting.SourceLocal)]
    [InlineData(ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes)]
    public void CreateCompositionSettings_Should_MapShareableFieldRuntimeTypeRouting(
        ShareableFieldRuntimeTypeRouting? routing)
    {
        var settings = new GraphQLCompositionSettings
        {
            ShareableFieldRuntimeTypeRouting = routing
        };

        var compositionSettings = AspireCompositionHelper.CreateCompositionSettings(settings);

        Assert.Equal(
            routing,
            compositionSettings.ApolloFederationCompatibility.ShareableFieldRuntimeTypeRouting);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateCompositionSettings_Should_MapAllowNonResolvableInterfaceObjects(
        bool? allow)
    {
        var settings = new GraphQLCompositionSettings
        {
            AllowNonResolvableInterfaceObjects = allow
        };

        var compositionSettings = AspireCompositionHelper.CreateCompositionSettings(settings);

        Assert.Equal(
            allow,
            compositionSettings.ApolloFederationCompatibility.AllowNonResolvableInterfaceObjects);
    }

    [Fact]
    public void CreateCompositionSettings_Should_MapAllUserFacingSettings()
    {
        var settings = new GraphQLCompositionSettings
        {
            AllowNonResolvableInterfaceObjects = true,
            CacheControlMergeBehavior = DirectiveMergeBehavior.IncludePrivate,
            EnableGlobalObjectIdentification = true,
            ExcludeByTag = new HashSet<string> { "internal" },
            IncludeSatisfiabilityPaths = false,
            NodeResolution = NodeResolution.SourceSchema,
            ShareableFieldRuntimeTypeRouting =
                ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes,
            TagMergeBehavior = DirectiveMergeBehavior.Include
        };
        var compositionSettings = AspireCompositionHelper.CreateCompositionSettings(settings);
        using var document = JsonSerializer.SerializeToDocument(
            compositionSettings,
            SettingsJsonSerializerContext.Default.CompositionSettings);
        var json = JsonSerializer.Serialize(
            document.RootElement,
            new JsonSerializerOptions { WriteIndented = true });

        json.MatchInlineSnapshot(
            """
            {
              "preprocessor": {
                "excludeByTag": [
                  "internal"
                ]
              },
              "merger": {
                "addFusionDefinitions": null,
                "cacheControlMergeBehavior": "IncludePrivate",
                "enableGlobalObjectIdentification": true,
                "nodeResolution": "SourceSchema",
                "removeUnreferencedDefinitions": null,
                "tagMergeBehavior": "Include"
              },
              "satisfiability": {
                "includeSatisfiabilityPaths": false
              },
              "apolloFederationCompatibility": {
                "allowNonResolvableInterfaceObjects": true,
                "shareableFieldRuntimeTypeRouting": "CommonRuntimeTypes"
              }
            }
            """);
    }

    [Fact]
    public void BuildLocalUrlOverrides_Should_UseConfiguredPath_When_SettingsDefineHttpUrl()
    {
        // arrange
        using var settings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "https://products.internal.example.com/api/graphql"
                }
              }
            }
            """);
        var sourceSchema = CreateSourceSchema(
            "Products",
            "http://localhost:5001",
            settings,
            ProductsSchemaText);

        // act
        var localUrlOverrides = AspireCompositionHelper.BuildLocalUrlOverrides(
            [sourceSchema],
            "Aspire",
            NullLogger<SchemaComposition>.Instance);

        // assert
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["Products"] = "http://localhost:5001/api/graphql"
            },
            localUrlOverrides);
    }

    [Fact]
    public void BuildLocalUrlOverrides_Should_UseDefaultPath_When_SettingsDefineNoHttpUrl()
    {
        // arrange
        using var settings = JsonDocument.Parse("""{ "name": "Products" }""");
        var sourceSchema = CreateSourceSchema(
            "Products",
            "http://localhost:5001/",
            settings,
            ProductsSchemaText);

        // act
        var localUrlOverrides = AspireCompositionHelper.BuildLocalUrlOverrides(
            [sourceSchema],
            "Aspire",
            NullLogger<SchemaComposition>.Instance);

        // assert
        Assert.Equal(
            new Dictionary<string, string> { ["Products"] = "http://localhost:5001/graphql" },
            localUrlOverrides);
    }

    [Fact]
    public void BuildLocalUrlOverrides_Should_ResolvePath_When_ConfiguredUrlContainsVariables()
    {
        // arrange
        using var settings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "{{API_URL}}"
                }
              },
              "environments": {
                "Aspire": {
                  "API_URL": "https://products.internal.example.com/api/graphql"
                }
              }
            }
            """);
        var sourceSchema = CreateSourceSchema(
            "Products",
            "http://localhost:5001",
            settings,
            ProductsSchemaText);

        // act
        var localUrlOverrides = AspireCompositionHelper.BuildLocalUrlOverrides(
            [sourceSchema],
            "Aspire",
            NullLogger<SchemaComposition>.Instance);

        // assert
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["Products"] = "http://localhost:5001/api/graphql"
            },
            localUrlOverrides);
    }

    [Fact]
    public void BuildLocalUrlOverrides_Should_UseDefaultPath_When_UrlVariableIsUnresolvable()
    {
        // arrange
        using var settings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "{{API_URL}}"
                }
              },
              "environments": {
                "Production": {
                  "API_URL": "https://products.internal.example.com/api/graphql"
                }
              }
            }
            """);
        var sourceSchema = CreateSourceSchema(
            "Products",
            "http://localhost:5001",
            settings,
            ProductsSchemaText);

        // act
        var localUrlOverrides = AspireCompositionHelper.BuildLocalUrlOverrides(
            [sourceSchema],
            "Aspire",
            NullLogger<SchemaComposition>.Instance);

        // assert
        Assert.Equal(
            new Dictionary<string, string> { ["Products"] = "http://localhost:5001/graphql" },
            localUrlOverrides);
    }

    [Fact]
    public void BuildLocalUrlOverrides_Should_SkipSchema_When_ResourceHasNoAllocatedEndpoint()
    {
        // arrange
        using var settings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "https://products.internal.example.com/graphql"
                }
              }
            }
            """);
        var sourceSchema = CreateSourceSchema(
            "Products",
            allocatedHttpEndpointUrl: null,
            settings,
            ProductsSchemaText);

        // act
        var localUrlOverrides = AspireCompositionHelper.BuildLocalUrlOverrides(
            [sourceSchema],
            "Aspire",
            NullLogger<SchemaComposition>.Instance);

        // assert
        Assert.Empty(localUrlOverrides);
    }

    [Fact]
    public async Task TryComposeAsync_Should_ComposeLocalUrlAndDevUrl_When_SchemasAreMixed()
    {
        // arrange
        var archivePath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName());
        using var productsSettings = JsonDocument.Parse(
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
        using var reviewsSettings = JsonDocument.Parse(
            """
            {
              "name": "Reviews",
              "transports": {
                "http": {
                  "url": "https://reviews.internal.example.com/graphql",
                  "devUrl": "https://reviews.dev.example.com/graphql"
                }
              }
            }
            """);
        var products = CreateSourceSchema(
            "Products",
            "http://localhost:5001",
            productsSettings,
            ProductsSchemaText);
        var reviews = CreateSourceSchema(
            "Reviews",
            allocatedHttpEndpointUrl: null,
            reviewsSettings,
            "type Query { review: String }");

        try
        {
            // act
            var success = await AspireCompositionHelper.TryComposeAsync(
                archivePath,
                seedArchivePath: null,
                [products, reviews],
                default,
                externalEnvironment: null,
                NullLogger<SchemaComposition>.Instance,
                TestContext.Current.CancellationToken);

            // assert
            Assert.True(success);
            using var archive = FusionArchive.Open(archivePath);
            using var gatewayConfiguration = await archive.TryGetGatewayConfigurationAsync(
                WellKnownVersions.LatestGatewayFormatVersion,
                TestContext.Current.CancellationToken);
            Assert.NotNull(gatewayConfiguration);
            JsonSerializer.Serialize(
                gatewayConfiguration.Settings.RootElement,
                new JsonSerializerOptions { WriteIndented = true }).MatchInlineSnapshot(
                """
                {
                  "sourceSchemas": {
                    "Products": {
                      "transports": {
                        "http": {
                          "url": "http://localhost:5001/graphql"
                        }
                      }
                    },
                    "Reviews": {
                      "transports": {
                        "http": {
                          "url": "https://reviews.dev.example.com/graphql"
                        }
                      }
                    }
                  }
                }
                """);
        }
        finally
        {
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }
        }
    }

    [Fact]
    public async Task TryComposeAsync_Should_PreserveFullFederationV1SourceSettings()
    {
        // arrange
        var archivePath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName());
        using var sourceSettings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "https://products.example.com/graphql",
                  "capabilities": {
                    "batching": {
                      "variableBatching": false,
                      "requestBatching": false
                    }
                  }
                }
              },
              "preprocessor": {
                "inferKeysFromLookups": false
              },
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "1.0"
                  }
                }
              }
            }
            """);
        var endpointConfiguration = SchemaComposition.ReadEndpointConfiguration(
            "products-resource",
            configuredSourceSchemaName: null,
            sourceSettings);

        try
        {
            // act
            var success = await AspireCompositionHelper.TryComposeAsync(
                archivePath,
                seedArchivePath: null,
                [
                    new SourceSchemaInfo
                    {
                        Name = endpointConfiguration.SourceSchemaName,
                        Schema = new SourceSchemaText(
                            endpointConfiguration.SourceSchemaName,
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
                              name: String
                            }
                            """),
                        SchemaSettings = sourceSettings
                    }
                ],
                default,
                externalEnvironment: null,
                NullLogger<SchemaComposition>.Instance,
                TestContext.Current.CancellationToken);

            // assert
            Assert.True(success);
            using var archive = FusionArchive.Open(archivePath);
            Assert.Equal(
                ["Products"],
                (await archive.GetSourceSchemaNamesAsync(
                    TestContext.Current.CancellationToken)).ToArray());
            using var configuration = await archive.TryGetSourceSchemaConfigurationAsync(
                endpointConfiguration.SourceSchemaName,
                TestContext.Current.CancellationToken);
            Assert.NotNull(configuration);
            configuration.Settings.RootElement.ToString().MatchInlineSnapshot(
                """
                {
                  "name": "Products",
                  "transports": {
                    "http": {
                      "url": "https://products.example.com/graphql",
                      "capabilities": {
                        "batching": {
                          "variableBatching": false,
                          "requestBatching": false
                        }
                      }
                    }
                  },
                  "preprocessor": {
                    "inferKeysFromLookups": false
                  },
                  "extensions": {
                    "chillicream": {
                      "apolloFederationSupport": {
                        "version": "1.0"
                      }
                    }
                  }
                }
                """);
        }
        finally
        {
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }
        }
    }

    [Fact]
    public async Task TryComposeAsync_Should_KeepTheEnvironmentsOfASourceSchema_When_ItIsStored()
    {
        // arrange
        // a fusion configuration carries the source schema settings as they are written, so the
        // environments of a source schema survive and can be resolved again for another
        // environment when the archive is composed onto.
        var archivePath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName());
        using var settings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "{{API_URL}}",
                  "devUrl": "{{DEV_API_URL}}"
                }
              },
              "environments": {
                "production": {
                  "API_URL": "https://products.example.com/graphql",
                  "DEV_API_URL": "https://products.dev.example.com/graphql"
                }
              }
            }
            """);
        var products = CreateSourceSchema(
            "Products",
            allocatedHttpEndpointUrl: null,
            settings,
            ProductsSchemaText);

        try
        {
            // act
            var success = await AspireCompositionHelper.TryComposeAsync(
                archivePath,
                seedArchivePath: null,
                [products],
                default,
                externalEnvironment: null,
                NullLogger<SchemaComposition>.Instance,
                TestContext.Current.CancellationToken);

            // assert
            Assert.True(success);
            using var archive = FusionArchive.Open(archivePath);
            using var configuration = await archive.TryGetSourceSchemaConfigurationAsync(
                "Products",
                TestContext.Current.CancellationToken);
            configuration!.Settings.RootElement.ToString().MatchInlineSnapshot(
                """
                {
                  "name": "Products",
                  "transports": {
                    "http": {
                      "url": "{{API_URL}}",
                      "devUrl": "{{DEV_API_URL}}"
                    }
                  },
                  "environments": {
                    "production": {
                      "API_URL": "https://products.example.com/graphql",
                      "DEV_API_URL": "https://products.dev.example.com/graphql"
                    }
                  }
                }
                """);
        }
        finally
        {
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }
        }
    }

    [Fact]
    public async Task TryComposeAsync_Should_ReplaceTheArchive_When_ASeedIsTheCompositionBase()
    {
        // arrange
        // the seed is the sole base of the composition, so a source schema that only exists in
        // the archive that is written does not survive.
        var archivePath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName());
        var seedArchivePath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName());
        using var productsSettings = JsonDocument.Parse("""{ "name": "Products" }""");
        using var legacySettings = JsonDocument.Parse("""{ "name": "Legacy" }""");
        var products = CreateSourceSchema(
            "Products",
            allocatedHttpEndpointUrl: null,
            productsSettings,
            ProductsSchemaText);
        var legacy = CreateSourceSchema(
            "Legacy",
            allocatedHttpEndpointUrl: null,
            legacySettings,
            "type Query { legacy: String }");

        try
        {
            // the previous output carries Legacy, the seed carries nothing but its metadata
            await AspireCompositionHelper.TryComposeAsync(
                archivePath,
                seedArchivePath: null,
                [legacy],
                default,
                externalEnvironment: null,
                NullLogger<SchemaComposition>.Instance,
                TestContext.Current.CancellationToken);
            using (var seedArchive = FusionArchive.Create(seedArchivePath))
            {
                await seedArchive.SetArchiveMetadataAsync(
                    new ArchiveMetadata
                    {
                        SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
                        SourceSchemas = []
                    },
                    TestContext.Current.CancellationToken);
                await seedArchive.CommitAsync(TestContext.Current.CancellationToken);
            }

            // act
            var success = await AspireCompositionHelper.TryComposeAsync(
                archivePath,
                seedArchivePath,
                [products],
                default,
                externalEnvironment: null,
                NullLogger<SchemaComposition>.Instance,
                TestContext.Current.CancellationToken);

            // assert
            Assert.True(success);
            using var archive = FusionArchive.Open(archivePath);
            Assert.Equal(
                ["Products"],
                (await archive.GetSourceSchemaNamesAsync(
                    TestContext.Current.CancellationToken)).ToArray());
        }
        finally
        {
            foreach (var path in (string[])[archivePath, seedArchivePath])
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    private static SourceSchemaInfo CreateSourceSchema(
        string name,
        string? allocatedHttpEndpointUrl,
        JsonDocument schemaSettings,
        string schemaText)
        => new()
        {
            Name = name,
            AllocatedHttpEndpointUrl = allocatedHttpEndpointUrl,
            Schema = new SourceSchemaText(name, schemaText),
            SchemaSettings = schemaSettings
        };
}

using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Aspire.Nitro;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.SourceSchema.Packaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

public sealed class AspireCompositionHelperTests
{
    private const string ProductsSchemaText = "type Query { product: String }";

    [Fact]
    public async Task TryComposeArchivesAsync_Should_ResolveSettings_WhenEnvironmentIsExplicit()
    {
        using var directory = new TestDirectory();
        var sourceArchivePath = IOPath.Combine(directory.Path, "products.zip");
        var stagingArchivePath = IOPath.Combine(directory.Path, "staging.far");
        var productionArchivePath = IOPath.Combine(directory.Path, "production.far");
        await CreateSourceArchiveAsync(sourceArchivePath);
        var sourceArchives =
            new[] { new SourceSchemaArchiveInfo("Products", sourceArchivePath) };
        var compositionSettings = new GraphQLCompositionSettings
        {
            EnvironmentName = "Aspire"
        };

        var stagingSuccess = await AspireCompositionHelper.TryComposeArchivesAsync(
            stagingArchivePath,
            sourceArchives,
            "Staging",
            compositionSettings,
            NullLogger<SchemaComposition>.Instance,
            TestContext.Current.CancellationToken);
        var productionSuccess = await AspireCompositionHelper.TryComposeArchivesAsync(
            productionArchivePath,
            sourceArchives,
            "Production",
            compositionSettings,
            NullLogger<SchemaComposition>.Instance,
            TestContext.Current.CancellationToken);

        Assert.True(stagingSuccess);
        Assert.True(productionSuccess);
        var stagingSettings = await ReadGatewaySettingsAsync(stagingArchivePath);
        var productionSettings = await ReadGatewaySettingsAsync(productionArchivePath);
        string.Join(
                Environment.NewLine,
                "## Staging",
                stagingSettings,
                "",
                "## Production",
                productionSettings)
            .MatchInlineSnapshot(
                """
                ## Staging
                {
                  "sourceSchemas": {
                    "Products": {
                      "transports": {
                        "http": {
                          "url": "https://staging.products.example.com/graphql",
                          "capabilities": {
                            "subscriptions": {
                              "supported": true
                            }
                          }
                        }
                      },
                      "extensions": {
                        "timeout": 5000,
                        "label": "staging-green"
                      }
                    }
                  }
                }

                ## Production
                {
                  "sourceSchemas": {
                    "Products": {
                      "transports": {
                        "http": {
                          "url": "https://products.example.com/graphql",
                          "capabilities": {
                            "subscriptions": {
                              "supported": false
                            }
                          }
                        }
                      },
                      "extensions": {
                        "timeout": 10000,
                        "label": "production-blue"
                      }
                    }
                  }
                }
                """);
    }

    [Theory]
    [InlineData("staging")]
    [InlineData("Preview")]
    public async Task TryComposeArchivesAsync_Should_Fail_WhenEnvironmentDoesNotProvideVariables(
        string environmentName)
    {
        using var directory = new TestDirectory();
        var sourceArchivePath = IOPath.Combine(directory.Path, "products.zip");
        var fusionArchivePath = IOPath.Combine(directory.Path, "gateway.far");
        await CreateSourceArchiveAsync(sourceArchivePath);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => AspireCompositionHelper.TryComposeArchivesAsync(
                fusionArchivePath,
                [new SourceSchemaArchiveInfo("Products", sourceArchivePath)],
                environmentName,
                default,
                NullLogger<SchemaComposition>.Instance,
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "Variable 'BASE_URL' not found in environment",
            exception.Message);
    }

    [Fact]
    public void ResolveSourceSchemaSettings_Should_RemoveEnvironmentMap_WhenEnvironmentIsResolved()
    {
        using var sourceSettings = CreateSourceSettings();
        using var resolved = AspireCompositionHelper.ResolveSourceSchemaSettings(
            sourceSettings,
            "Staging");

        JsonSerializer.Serialize(
                resolved.RootElement,
                new JsonSerializerOptions { WriteIndented = true })
            .MatchInlineSnapshot(
                """
                {
                  "transports": {
                    "http": {
                      "url": "https://staging.products.example.com/graphql",
                      "capabilities": {
                        "subscriptions": {
                          "supported": true
                        }
                      }
                    }
                  },
                  "extensions": {
                    "timeout": 5000,
                    "label": "staging-green"
                  }
                }
                """);
    }

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

        var compositionSettings = AspireCompositionHelper.CreateCompositionSettings(
            settings,
            stageSettings: null);

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

        var compositionSettings = AspireCompositionHelper.CreateCompositionSettings(
            settings,
            stageSettings: null);

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

        var compositionSettings = AspireCompositionHelper.CreateCompositionSettings(
            settings,
            stageSettings: null);

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
            EnumValuesMergeBehavior = EnumValuesMergeBehavior.Union,
            ExcludeByTag = new HashSet<string> { "internal" },
            IncludeSatisfiabilityPaths = false,
            NodeResolution = NodeResolution.SourceSchema,
            ShareableFieldRuntimeTypeRouting = ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes,
            TagMergeBehavior = DirectiveMergeBehavior.Include
        };
        var compositionSettings = AspireCompositionHelper.CreateCompositionSettings(
            settings,
            stageSettings: null);
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
                "enumValuesMergeBehavior": "Union",
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
    public void CreateCompositionSettings_Should_UseStageSettings_When_SettingsAreUnset()
    {
        // arrange
        var settings = new GraphQLCompositionSettings
        {
            TagMergeBehavior = DirectiveMergeBehavior.Include
        };
        var stageSettings = new CompositionSettings
        {
            Merger = new CompositionSettings.MergerSettings
            {
                CacheControlMergeBehavior = DirectiveMergeBehavior.IncludePrivate,
                EnableGlobalObjectIdentification = true,
                NodeResolution = NodeResolution.SourceSchema,
                RemoveUnreferencedDefinitions = true,
                TagMergeBehavior = DirectiveMergeBehavior.Ignore
            },
            Preprocessor = new CompositionSettings.PreprocessorSettings
            {
                ExcludeByTag = ["internal"]
            }
        };

        // act
        var compositionSettings = AspireCompositionHelper.CreateCompositionSettings(
            settings,
            stageSettings);

        // assert
        SerializeSettings(compositionSettings)
            .MatchInlineSnapshot(
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
                    "enumValuesMergeBehavior": null,
                    "nodeResolution": "SourceSchema",
                    "removeUnreferencedDefinitions": true,
                    "tagMergeBehavior": "Include"
                  },
                  "satisfiability": {
                    "includeSatisfiabilityPaths": null
                  },
                  "apolloFederationCompatibility": {
                    "allowNonResolvableInterfaceObjects": null,
                    "shareableFieldRuntimeTypeRouting": null
                  }
                }
                """);
    }

    [Fact]
    public void CreateCompositionSettings_Should_KeepSettings_When_StageSettingsDeclareThemToo()
    {
        // arrange
        var settings = new GraphQLCompositionSettings
        {
            CacheControlMergeBehavior = DirectiveMergeBehavior.Ignore,
            EnableGlobalObjectIdentification = false,
            ExcludeByTag = new HashSet<string> { "local" },
            NodeResolution = NodeResolution.Gateway
        };
        var stageSettings = new CompositionSettings
        {
            Merger = new CompositionSettings.MergerSettings
            {
                CacheControlMergeBehavior = DirectiveMergeBehavior.IncludePrivate,
                EnableGlobalObjectIdentification = true,
                NodeResolution = NodeResolution.SourceSchema
            },
            Preprocessor = new CompositionSettings.PreprocessorSettings
            {
                ExcludeByTag = ["stage"]
            }
        };

        // act
        var compositionSettings = AspireCompositionHelper.CreateCompositionSettings(
            settings,
            stageSettings);

        // assert
        SerializeSettings(compositionSettings)
            .MatchInlineSnapshot(
                """
                {
                  "preprocessor": {
                    "excludeByTag": [
                      "local"
                    ]
                  },
                  "merger": {
                    "addFusionDefinitions": null,
                    "cacheControlMergeBehavior": "Ignore",
                    "enableGlobalObjectIdentification": false,
                    "enumValuesMergeBehavior": null,
                    "nodeResolution": "Gateway",
                    "removeUnreferencedDefinitions": null,
                    "tagMergeBehavior": null
                  },
                  "satisfiability": {
                    "includeSatisfiabilityPaths": null
                  },
                  "apolloFederationCompatibility": {
                    "allowNonResolvableInterfaceObjects": null,
                    "shareableFieldRuntimeTypeRouting": null
                  }
                }
                """);
    }

    [Fact]
    public void TryBuildLocalSourceSchemas_Should_TrimSlash_When_TheAllocatedEndpointEndsWithOne()
    {
        // arrange
        using var settings = JsonDocument.Parse("""{ "name": "Products" }""");
        var sourceSchema = CreateSourceSchema(
            "Products",
            "http://localhost:5001/",
            settings,
            ProductsSchemaText) with { GraphQLPath = "/api/graphql" };

        // act
        var success = AspireCompositionHelper.TryBuildLocalSourceSchemas(
            [sourceSchema],
            NullLogger<SchemaComposition>.Instance,
            out var localSourceSchemas);

        // assert
        Assert.True(success);
        Assert.Equal(
            new Uri("http://localhost:5001/api/graphql"),
            localSourceSchemas["Products"].UrlOverride);
    }

    [Fact]
    public void TryBuildLocalSourceSchemas_Should_UseDeclaredPath_When_SourceSchemaDeclaresGraphQLPath()
    {
        // arrange
        // the path of the configured HTTP transport URL does not contribute to the override.
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
            ProductsSchemaText) with { GraphQLPath = "/declared/graphql" };

        // act
        var success = AspireCompositionHelper.TryBuildLocalSourceSchemas(
            [sourceSchema],
            NullLogger<SchemaComposition>.Instance,
            out var localSourceSchemas);

        // assert
        Assert.True(success);
        Assert.Equal(
            new Uri("http://localhost:5001/declared/graphql"),
            localSourceSchemas["Products"].UrlOverride);
    }

    [Fact]
    public void TryBuildLocalSourceSchemas_Should_Throw_When_SourceSchemaNamesAreDuplicated()
    {
        // arrange
        using var settings = JsonDocument.Parse("""{ "name": "Products" }""");
        var first = CreateSourceSchema(
            "Products",
            "http://localhost:5001",
            settings,
            ProductsSchemaText);
        var second = CreateSourceSchema(
            "Products",
            "http://localhost:5002",
            settings,
            ProductsSchemaText);

        // act
        void Act() => AspireCompositionHelper.TryBuildLocalSourceSchemas(
            [first, second],
            NullLogger<SchemaComposition>.Instance,
            out _);

        // assert
        var exception = Assert.Throws<ArgumentException>(Act);
        Assert.Equal(
            "An item with the same key has already been added. Key: Products",
            exception.Message);
    }

    [Fact]
    public void TryBuildLocalSourceSchemas_Should_LeaveUrlOverrideNull_When_ResourceHasNoAllocatedEndpoint()
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
        var success = AspireCompositionHelper.TryBuildLocalSourceSchemas(
            [sourceSchema],
            NullLogger<SchemaComposition>.Instance,
            out var localSourceSchemas);

        // assert
        Assert.True(success);
        Assert.Null(localSourceSchemas["Products"].UrlOverride);
    }

    [Fact]
    public void TryBuildLocalSourceSchemas_Should_Fail_When_ASourceSchemaDeclaresNoGraphQLPath()
    {
        // arrange
        // a resource that is registered with the retired WithGraphQLSchemaFile or
        // WithGraphQLSchemaEndpoint API declares no GraphQL path.
        using var settings = JsonDocument.Parse("""{ "name": "Products" }""");
        var sourceSchema = CreateSourceSchema(
            "Products",
            "http://localhost:5001",
            settings,
            ProductsSchemaText) with { ResourceName = "products", GraphQLPath = null };
        var logger = new RecordingLogger<SchemaComposition>();

        // act
        var success = AspireCompositionHelper.TryBuildLocalSourceSchemas(
            [sourceSchema],
            logger,
            out _);

        // assert
        Assert.False(success);
        Assert.Equal(
            "The source schema Products of the resource products does not declare the path of "
            + "its GraphQL endpoint. Call WithGraphQLHttpEndpoint on the resource.",
            Assert.Single(logger.Entries, entry => entry.Level is LogLevel.Error).Message);
    }

    [Fact]
    public void TryBuildLocalSourceSchemas_Should_ReportEverySchemaWithoutAPath_When_SchemasAreMixed()
    {
        // arrange
        using var settings = JsonDocument.Parse("""{ "name": "Products" }""");
        var migrated = CreateSourceSchema(
            "Products",
            "http://localhost:5001",
            settings,
            ProductsSchemaText) with { ResourceName = "products" };
        var legacy = CreateSourceSchema(
            "Reviews",
            "http://localhost:5002",
            settings,
            "type Query { review: String }") with { ResourceName = "reviews", GraphQLPath = null };
        var alsoLegacy = CreateSourceSchema(
            "Orders",
            allocatedHttpEndpointUrl: null,
            settings,
            "type Query { order: String }") with { GraphQLPath = null };
        var logger = new RecordingLogger<SchemaComposition>();

        // act
        var success = AspireCompositionHelper.TryBuildLocalSourceSchemas(
            [migrated, legacy, alsoLegacy],
            logger,
            out _);

        // assert
        Assert.False(success);
        string.Join(
            Environment.NewLine,
            logger.Entries
                .Where(entry => entry.Level is LogLevel.Error)
                .Select(entry => entry.Message)).MatchInlineSnapshot(
            """
            The source schema Reviews of the resource reviews does not declare the path of its GraphQL endpoint. Call WithGraphQLHttpEndpoint on the resource.
            The source schema Orders does not declare the path of its GraphQL endpoint. Call WithGraphQLHttpEndpoint on the resource that serves it.
            """);
    }

    [Fact]
    public async Task TryComposeAsync_Should_Fail_When_ASourceSchemaDeclaresNoGraphQLPath()
    {
        // arrange
        var archivePath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName());
        using var settings = JsonDocument.Parse("""{ "name": "Products" }""");
        var products = CreateSourceSchema(
            "Products",
            "http://localhost:5001",
            settings,
            ProductsSchemaText) with { ResourceName = "products", GraphQLPath = null };
        var logger = new RecordingLogger<SchemaComposition>();

        try
        {
            // act
            var success = await AspireCompositionHelper.TryComposeAsync(
                archivePath,
                seedArchivePath: null,
                [products],
                default,
                environment: null,
                logger,
                TestContext.Current.CancellationToken);

            // assert
            $"""
            Success: {success}
            Archive written: {File.Exists(archivePath)}
            Errors:
            {string.Join(
                Environment.NewLine,
                logger.Entries
                    .Where(entry => entry.Level is LogLevel.Error)
                    .Select(entry => entry.Message))}
            """.MatchInlineSnapshot(
                """
                Success: False
                Archive written: False
                Errors:
                The source schema Products of the resource products does not declare the path of its GraphQL endpoint. Call WithGraphQLHttpEndpoint on the resource.
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
                environment: null,
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
                        GraphQLPath = "/graphql",
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
                environment: null,
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

    private static string SerializeSettings(CompositionSettings settings)
    {
        using var document = JsonSerializer.SerializeToDocument(
            settings,
            SettingsJsonSerializerContext.Default.CompositionSettings);

        return JsonSerializer.Serialize(
            document.RootElement,
            new JsonSerializerOptions { WriteIndented = true });
    }

    private static async Task CreateSourceArchiveAsync(string archivePath)
    {
        using var settings = CreateSourceSettings();
        using var archive = FusionSourceSchemaArchive.Create(archivePath);
        await archive.SetArchiveMetadataAsync(
            new HotChocolate.Fusion.SourceSchema.Packaging.ArchiveMetadata(),
            TestContext.Current.CancellationToken);
        await archive.SetSchemaAsync(
            Encoding.UTF8.GetBytes(
                """
                type Query {
                  product: Product
                }

                type Product {
                  id: ID!
                  name: String!
                }
                """),
            TestContext.Current.CancellationToken);
        await archive.SetSettingsAsync(
            settings,
            TestContext.Current.CancellationToken);
        await archive.CommitAsync(TestContext.Current.CancellationToken);
    }

    private static JsonDocument CreateSourceSettings()
        => JsonDocument.Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "{{BASE_URL}}/graphql",
                  "capabilities": {
                    "subscriptions": {
                      "supported": "{{SUBSCRIPTIONS_ENABLED}}"
                    }
                  }
                }
              },
              "extensions": {
                "timeout": "{{TIMEOUT}}",
                "label": "{{ENVIRONMENT}}-{{COLOR}}"
              },
              "environments": {
                "Staging": {
                  "BASE_URL": "https://staging.products.example.com",
                  "SUBSCRIPTIONS_ENABLED": true,
                  "TIMEOUT": 5000,
                  "ENVIRONMENT": "staging",
                  "COLOR": "green"
                },
                "Production": {
                  "BASE_URL": "https://products.example.com",
                  "SUBSCRIPTIONS_ENABLED": false,
                  "TIMEOUT": 10000,
                  "ENVIRONMENT": "production",
                  "COLOR": "blue"
                }
              }
            }
            """);

    private static async Task<string> ReadGatewaySettingsAsync(
        string fusionArchivePath)
    {
        using var archive = FusionArchive.Open(fusionArchivePath);
        using var configuration = await archive.TryGetGatewayConfigurationAsync(
            new Version(99, 0),
            TestContext.Current.CancellationToken);
        Assert.NotNull(configuration);
        return JsonSerializer.Serialize(
            configuration.Settings.RootElement,
            new JsonSerializerOptions { WriteIndented = true });
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "fusion-aspire-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
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
                environment: null,
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
                environment: null,
                NullLogger<SchemaComposition>.Instance,
                TestContext.Current.CancellationToken);
            using (var seedArchive = FusionArchive.Create(seedArchivePath))
            {
                await seedArchive.SetArchiveMetadataAsync(
                    new HotChocolate.Fusion.Packaging.ArchiveMetadata
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
                environment: null,
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

    [Theory]
    [InlineData("stage", "custom", "https://stage.example.com/graphql")]
    [InlineData(null, "custom", "https://custom.example.com/graphql")]
    [InlineData(null, null, "https://aspire.example.com/graphql")]
    public async Task TryComposeAsync_Should_ResolveAgainstStageThenEnvironmentNameThenAspire_When_EnvironmentInputsVary(
        string? environment,
        string? environmentName,
        string expectedUrl)
    {
        // arrange
        var archivePath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName());
        using var settingsDocument = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "https://fallback.example.com/graphql",
                  "devUrl": "{{API_URL}}"
                }
              },
              "environments": {
                "stage": { "API_URL": "https://stage.example.com/graphql" },
                "custom": { "API_URL": "https://custom.example.com/graphql" },
                "Aspire": { "API_URL": "https://aspire.example.com/graphql" }
              }
            }
            """);
        var products = CreateSourceSchema(
            "Products",
            allocatedHttpEndpointUrl: null,
            settingsDocument,
            ProductsSchemaText);
        var compositionSettings = new GraphQLCompositionSettings();
        // The obsolete EnvironmentName setting is honored during the deprecation window.
#pragma warning disable CS0618
        compositionSettings.EnvironmentName = environmentName;
#pragma warning restore CS0618

        try
        {
            // act
            var success = await AspireCompositionHelper.TryComposeAsync(
                archivePath,
                seedArchivePath: null,
                [products],
                compositionSettings,
                environment,
                NullLogger<SchemaComposition>.Instance,
                TestContext.Current.CancellationToken);

            // assert
            Assert.True(success);
            using var archive = FusionArchive.Open(archivePath);
            using var gatewayConfiguration = await archive.TryGetGatewayConfigurationAsync(
                WellKnownVersions.LatestGatewayFormatVersion,
                TestContext.Current.CancellationToken);
            Assert.NotNull(gatewayConfiguration);
            var composedUrl = gatewayConfiguration.Settings.RootElement
                .GetProperty("sourceSchemas")
                .GetProperty("Products")
                .GetProperty("transports")
                .GetProperty("http")
                .GetProperty("url")
                .GetString();
            Assert.Equal(expectedUrl, composedUrl);
        }
        finally
        {
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }
        }
    }

    /// <summary>
    /// Creates a source schema of a resource that is registered with WithGraphQLHttpEndpoint,
    /// which declares the default GraphQL path.
    /// </summary>
    private static SourceSchemaInfo CreateSourceSchema(
        string name,
        string? allocatedHttpEndpointUrl,
        JsonDocument schemaSettings,
        string schemaText)
        => new()
        {
            Name = name,
            AllocatedHttpEndpointUrl = allocatedHttpEndpointUrl,
            GraphQLPath = "/graphql",
            Schema = new SourceSchemaText(name, schemaText),
            SchemaSettings = schemaSettings
        };
}

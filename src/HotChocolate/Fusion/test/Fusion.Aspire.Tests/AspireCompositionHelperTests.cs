using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.SourceSchema.Packaging;
using Microsoft.Extensions.Logging.Abstractions;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

public sealed class AspireCompositionHelperTests
{
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
    public async Task TryComposeAsync_Should_PreserveFullFederationV1SourceSettings()
    {
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
            var success = await AspireCompositionHelper.TryComposeAsync(
                archivePath,
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
                NullLogger<SchemaComposition>.Instance,
                TestContext.Current.CancellationToken);

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
}

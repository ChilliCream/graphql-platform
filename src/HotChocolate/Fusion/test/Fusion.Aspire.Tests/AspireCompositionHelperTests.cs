using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Packaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotChocolate.Fusion.Aspire;

public sealed class AspireCompositionHelperTests
{
    private const string ProductsSchemaText = "type Query { product: String }";

    [Fact]
    public async Task TryComposeAsync_Should_RemoveSignatureAndWarn_When_ExistingArchiveIsSigned()
    {
        // arrange
        var archivePath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName());
        using var rsa = RSA.Create(2048);
        using var certificate = new CertificateRequest(
            "CN=Test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1).CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(1));
        using var productsSettings = JsonDocument.Parse("""{ "name": "Products" }""");
        var products = CreateSourceSchema(
            "Products",
            allocatedHttpEndpointUrl: null,
            productsSettings,
            ProductsSchemaText);
        var logger = new RecordingLogger<SchemaComposition>();

        try
        {
            using (var archive = FusionArchive.Create(archivePath))
            {
                await archive.SetArchiveMetadataAsync(
                    new ArchiveMetadata
                    {
                        SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
                        SourceSchemas = []
                    },
                    TestContext.Current.CancellationToken);
                await archive.SignArchiveAsync(certificate, TestContext.Current.CancellationToken);
                await archive.CommitAsync(TestContext.Current.CancellationToken);
            }

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
            Assert.True(success);
            using var readArchive = FusionArchive.Open(archivePath);
            Assert.False(readArchive.IsSigned);
            Assert.Equal(
                "The Fusion archive signature was removed before composition. The producer must re-sign the archive.",
                Assert.Single(logger.Entries, entry => entry.Level is LogLevel.Warning).Message);
        }
        finally
        {
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }
        }
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
            EnumValuesMergeBehavior = EnumValuesMergeBehavior.Union,
            ExcludeByTag = new HashSet<string> { "internal" },
            IncludeSatisfiabilityPaths = false,
            NodeResolution = NodeResolution.SourceSchema,
            ShareableFieldRuntimeTypeRouting = ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes,
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

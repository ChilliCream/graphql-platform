using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using HotChocolate.Fusion.Aspire;

namespace ChilliCream.Nitro.Aspire;

#pragma warning disable ASPIREPIPELINES001

public sealed class FusionPipelineTests
{
    [Fact]
    public void SelectDeployments_Should_SelectOnlyMatchingEnvironment()
    {
        var builder = DistributedApplication.CreateBuilder();
        var nitro = builder
            .AddNitro("nitro")
            .WithCloudUrl("https://api.chillicream.com")
            .WithApiId("products");
        nitro
            .AddFusionDeployment("production")
            .ForEnvironment("Production")
            .ToStage("production")
            .WithConfigurationTag("release-1");
        nitro
            .AddFusionDeployment("staging")
            .ForEnvironment("Staging")
            .ToStage("staging")
            .WithConfigurationTag("release-1");
        var model = new DistributedApplicationModel(builder.Resources);

        var deployments = FusionPipeline.SelectDeployments(
            model,
            "Production");

        Assert.Equal(["production"], deployments.Select(x => x.Name));
    }

    [Fact]
    public void WithFusionReleaseManifest_Should_ConfigureExactParameterAndCompositionEnvironment()
    {
        var builder = DistributedApplication.CreateBuilder();
        var manifest = builder.AddParameter("fusionReleaseManifest");
        var deployment = builder
            .AddNitro("nitro")
            .AddFusionDeployment("production")
            .WithFusionReleaseManifest(manifest)
            .WithCompositionEnvironment("prod");

        Assert.Same(
            manifest.Resource,
            deployment.Resource.FusionReleaseManifestParameter);
        Assert.Equal(
            "prod",
            deployment.Resource.CompositionEnvironmentName);
    }

    [Fact]
    public void WithDefaultSourceVersionFromGitCommit_Should_Fail_WhenManifestIsConfigured()
    {
        var builder = DistributedApplication.CreateBuilder();
        var deployment = builder
            .AddNitro("nitro")
            .AddFusionDeployment("production")
            .WithFusionReleaseManifest(
                builder.AddParameter("fusionReleaseManifest"));

        var exception = Assert.Throws<InvalidOperationException>(
            deployment.WithDefaultSourceVersionFromGitCommit);

        Assert.Equal(
            "A promoted Fusion release cannot rediscover source versions from Git.",
            exception.Message);
    }

    [Fact]
    public void WithCloudUrl_Should_Fail_WhenUrlContainsCaseSensitivePath()
    {
        var builder = DistributedApplication.CreateBuilder();

        var exception = Assert.Throws<ArgumentException>(
            () => builder
                .AddNitro("nitro")
                .WithCloudUrl(
                    "https://api.chillicream.com/CaseSensitivePath"));

        Assert.Equal(
            "The Nitro cloud URL must be an absolute HTTPS origin without "
            + "a path, query, fragment, or user information. (Parameter 'cloudUrl')",
            exception.Message);
    }

    [Fact]
    public void SelectDeployments_Should_ReturnEmpty_WhenEnvironmentDoesNotMatch()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder
            .AddNitro("nitro")
            .WithCloudUrl("https://api.chillicream.com")
            .WithApiId("products")
            .AddFusionDeployment("production")
            .ForEnvironment("Production")
            .ToStage("production")
            .WithConfigurationTag("release-1");
        var model = new DistributedApplicationModel(builder.Resources);

        var deployments = FusionPipeline.SelectDeployments(
            model,
            "Development");

        Assert.Empty(deployments);
    }

    [Fact]
    public void ShouldUseManifestProducer_Should_PreserveLegacyMode_WhenAnotherEnvironmentUsesManifest()
    {
        var builder = DistributedApplication.CreateBuilder();
        var nitro = builder
            .AddNitro("nitro")
            .WithCloudUrl("https://api.chillicream.com")
            .WithApiId("products");
        nitro
            .AddFusionDeployment("development")
            .ForEnvironment("Development")
            .ToStage("development")
            .WithConfigurationTag("release-1");
        nitro
            .AddFusionDeployment("production")
            .ForEnvironment("Production")
            .ToStage("production")
            .WithConfigurationTag("release-1")
            .WithFusionReleaseManifest(
                builder.AddParameter("fusionReleaseManifest"));
        var model = new DistributedApplicationModel(builder.Resources);

        string.Join(
                Environment.NewLine,
                $"Development: {FusionPipeline.ShouldUseManifestProducer(model, "Development")}",
                $"Production: {FusionPipeline.ShouldUseManifestProducer(model, "Production")}",
                $"Release: {FusionPipeline.ShouldUseManifestProducer(model, "Release")}")
            .MatchInlineSnapshot(
                """
                Development: False
                Production: True
                Release: True
                """);
    }

    [Fact]
    public void SelectDeployments_Should_Fail_WhenMappingIsAmbiguous()
    {
        var builder = DistributedApplication.CreateBuilder();
        var nitro = builder
            .AddNitro("nitro")
            .WithCloudUrl("https://api.chillicream.com")
            .WithApiId("products");
        nitro
            .AddFusionDeployment("production-a")
            .ForEnvironment("Production")
            .ToStage("production")
            .WithConfigurationTag("release-1");
        nitro
            .AddFusionDeployment("production-b")
            .ForEnvironment("Production")
            .ToStage("production")
            .WithConfigurationTag("release-1");
        var model = new DistributedApplicationModel(builder.Resources);

        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionPipeline.SelectDeployments(model, "Production"));

        Assert.Equal(
            "Multiple Fusion deployments map environment 'Production' to Nitro "
            + "API 'products' stage 'production'.",
            exception.Message);
    }

    [Fact]
    public void CreateStepDefinitions_Should_WireArtifactAndRemoteRoots()
    {
        var resource = new FusionPipelineResource("fusion-pipeline");

        var steps = FusionPipeline.CreateStepDefinitionsForTest(resource);

        string.Join(
            Environment.NewLine,
            steps.Select(step =>
                $"{step.Name}: depends=[{string.Join(", ", step.DependsOnSteps)}]; "
                + $"requiredBy=[{string.Join(", ", step.RequiredBySteps)}]"))
            .MatchInlineSnapshot(
                """
                fusion-artifacts: depends=[]; requiredBy=[publish]
                fusion-upload: depends=[fusion-artifacts]; requiredBy=[]
                fusion-readiness: depends=[fusion-artifacts]; requiredBy=[]
                fusion-publish: depends=[fusion-upload, fusion-readiness]; requiredBy=[deploy]
                """);
    }

    [Fact]
    public void CreateStepDefinitions_Should_IsolateManifestApplyFromBuildSteps()
    {
        var resource = new FusionPipelineResource("fusion-pipeline");

        var steps = FusionPipeline.CreateStepDefinitionsForTest(
            resource,
            useManifestApply: true);

        string.Join(
                Environment.NewLine,
                steps.Select(step =>
                    $"{step.Name}: depends=[{string.Join(", ", step.DependsOnSteps)}]; "
                    + $"requiredBy=[{string.Join(", ", step.RequiredBySteps)}]"))
            .MatchInlineSnapshot(
                """
                fusion-artifacts: depends=[]; requiredBy=[publish]
                fusion-upload: depends=[fusion-artifacts]; requiredBy=[]
                fusion-release-prepare: depends=[]; requiredBy=[]
                fusion-compose: depends=[fusion-release-prepare]; requiredBy=[]
                fusion-readiness: depends=[fusion-compose]; requiredBy=[]
                fusion-publish: depends=[fusion-readiness]; requiredBy=[deploy]
                """);
    }

    [Fact]
    public void CreateStepDefinitions_Should_NotReachExportGitOrUpload_WhenApplyingManifest()
    {
        var steps = FusionPipeline.CreateStepDefinitionsForTest(
            new FusionPipelineResource("fusion-pipeline"),
            useManifestApply: true);
        var stepsByName = steps.ToDictionary(
            step => step.Name,
            StringComparer.Ordinal);

        string.Join(
                Environment.NewLine,
                GetTransitiveDependencies(
                        stepsByName,
                        FusionPipeline.PublishStepName)
                    .Order(StringComparer.Ordinal))
            .MatchInlineSnapshot(
                """
                fusion-compose
                fusion-readiness
                fusion-release-prepare
                """);
    }

    [Fact]
    public void CreateStepDefinitions_Should_NotAttachPublishToDeploy_WhenEnvironmentIsBuildOnly()
    {
        var steps = FusionPipeline.CreateStepDefinitionsForTest(
            new FusionPipelineResource("fusion-pipeline"),
            buildOnlyManifestProducer: true);

        string.Join(
                Environment.NewLine,
                steps.Select(step =>
                    $"{step.Name}: depends=[{string.Join(", ", step.DependsOnSteps)}]; "
                    + $"requiredBy=[{string.Join(", ", step.RequiredBySteps)}]"))
            .MatchInlineSnapshot(
                """
                fusion-artifacts: depends=[]; requiredBy=[publish]
                fusion-upload: depends=[fusion-artifacts]; requiredBy=[]
                fusion-readiness: depends=[]; requiredBy=[]
                fusion-publish: depends=[fusion-readiness]; requiredBy=[]
                """);
    }

    [Fact]
    public void ResolveSourceSchemaSettings_Should_UseDifferentEnvironmentOverrides_WhenSourceIsShared()
    {
        using var sourceSettings = JsonDocument.Parse(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "{{PRODUCTS_URL}}/graphql"
                }
              },
              "environments": {
                "staging": {
                  "PRODUCTS_URL": "https://products.staging.example.com"
                },
                "production": {
                  "PRODUCTS_URL": "https://products.example.com"
                }
              }
            }
            """);

        using var staging =
            FusionPipelineExecutor.ResolveSourceSchemaSettings(
                sourceSettings,
                "staging");
        using var production =
            FusionPipelineExecutor.ResolveSourceSchemaSettings(
                sourceSettings,
                "production");

        string.Join(
                Environment.NewLine,
                "source: products@release-1",
                $"staging: {GetHttpUrl(staging)}",
                $"production: {GetHttpUrl(production)}")
            .MatchInlineSnapshot(
                """
                source: products@release-1
                staging: https://products.staging.example.com/graphql
                production: https://products.example.com/graphql
                """);
    }

    [Fact]
    public void ResolveCompositionEnvironment_Should_UseStage_WhenNoOverrideExists()
    {
        var deployment = new FusionDeploymentResource(
            "production",
            new NitroResource("nitro"))
        {
            StageName = "production"
        };

        var environment =
            FusionPipelineExecutor.ResolveCompositionEnvironment(
                deployment,
                new GraphQLCompositionSettings());

        Assert.Equal("production", environment);
    }

    [Fact]
    public async Task WriteFinalAsync_Should_PreserveExistingManifest_WhenContentChanges()
    {
        using var testDirectory = new TestDirectory();
        var manifest = CreateReleaseManifest();
        await FusionReleaseStore.WriteFinalAsync(
            testDirectory.Path,
            manifest,
            TestContext.Current.CancellationToken);
        var manifestPath = Path.Combine(
            testDirectory.Path,
            "fusion-release.json");
        var original = await File.ReadAllBytesAsync(
            manifestPath,
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => FusionReleaseStore.WriteFinalAsync(
                testDirectory.Path,
                manifest with { ReleaseId = "release-2" },
                TestContext.Current.CancellationToken));

        Assert.Equal(
            $"Fusion release manifest '{manifestPath}' already exists with different content.",
            exception.Message);
        Assert.Equal(
            original,
            await File.ReadAllBytesAsync(
                manifestPath,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task VerifyArchiveAsync_Should_Fail_WhenArtifactIntegrityDoesNotMatch()
    {
        using var testDirectory = new TestDirectory();
        var manifest = CreateReleaseManifest();
        var source = Assert.Single(manifest.Sources);
        var archivePath = Path.Combine(
            testDirectory.Path,
            source.ArchivePath);
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        await File.WriteAllTextAsync(
            archivePath,
            "tampered",
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => FusionReleaseStore.VerifyArchiveAsync(
                archivePath,
                source,
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "Fusion source 'products' archive SHA-256 does not match the release manifest.",
            exception.Message);
    }

    [Fact]
    public async Task VerifyFileDigestAsync_Should_Fail_WhenComposedArchiveChanges()
    {
        using var testDirectory = new TestDirectory();
        var farPath = Path.Combine(
            testDirectory.Path,
            "fusion-configuration.far");
        await File.WriteAllTextAsync(
            farPath,
            "tampered",
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => FusionPipelineExecutor.VerifyFileDigestAsync(
                farPath,
                new string('0', 64),
                "composed Fusion archive",
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "The composed Fusion archive SHA-256 does not match prepared apply state.",
            exception.Message);
    }

    [Fact]
    public async Task ReadFinalAsync_Should_Fail_WhenManifestPathIsRelative()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => FusionReleaseStore.ReadFinalAsync(
                "fusion-release.json",
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "The Fusion release manifest parameter must resolve to an absolute path.",
            exception.Message);
    }

    [Fact]
    public void ValidateCompositionToolVersion_Should_Fail_WhenVersionDoesNotMatch()
    {
        var manifest = CreateReleaseManifest() with
        {
            CompositionToolVersion = "incompatible"
        };

        var exception = Assert.Throws<InvalidDataException>(
            () => FusionReleaseCompatibility.ValidateCompositionToolVersion(
                manifest));

        Assert.Equal(
            "Fusion release 'release-1' was created with composition tool "
            + "version 'incompatible', but apply is running version "
            + $"'{FusionReleaseCompatibility.CompositionToolVersion}'.",
            exception.Message);
    }

    [Fact]
    public async Task ReadFinalAsync_Should_ReturnInvalidData_WhenRequiredJsonIsNull()
    {
        using var testDirectory = new TestDirectory();
        var manifestPath = Path.Combine(
            testDirectory.Path,
            "fusion-release.json");
        await File.WriteAllTextAsync(
            manifestPath,
            """
            {
              "formatVersion": 1,
              "releaseId": "release-1",
              "sourceSetSha256": null,
              "composition": null,
              "sources": null,
              "targets": null
            }
            """,
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => FusionReleaseStore.ReadFinalAsync(
                manifestPath,
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "The Fusion release manifest is missing required content.",
            exception.Message);
    }

    [Theory]
    [InlineData(
        "\"name\": \"products\"",
        "\"name\": null",
        "The Fusion release manifest contains an invalid source.")]
    [InlineData(
        "\"cloudUrl\": \"https://api.chillicream.com\"",
        "\"cloudUrl\": null",
        "The Fusion release manifest contains an invalid target.")]
    [InlineData(
        "\"apiId\": \"products\"",
        "\"apiId\": null",
        "The Fusion release manifest contains an invalid target.")]
    public async Task ReadFinalAsync_Should_ReturnInvalidData_WhenNestedStringIsNull(
        string oldValue,
        string newValue,
        string expectedMessage)
    {
        using var testDirectory = new TestDirectory();
        var manifestPath = Path.Combine(
            testDirectory.Path,
            "fusion-release.json");
        var json = JsonSerializer.Serialize(
                CreateReleaseManifest(),
                FusionReleaseStore.SerializerOptions)
            .Replace(
                oldValue,
                newValue,
                StringComparison.Ordinal);
        await File.WriteAllTextAsync(
            manifestPath,
            json,
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => FusionReleaseStore.ReadFinalAsync(
                manifestPath,
                TestContext.Current.CancellationToken));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void Validate_Should_Fail_WhenArchivePathEscapesRelease()
    {
        var manifest = CreateReleaseManifest();
        var source = Assert.Single(manifest.Sources);
        var invalidSource = source with
        {
            ArchivePath = "../secret.zip"
        };
        var invalidSources = new[] { invalidSource };
        var sourceSetSha256 =
            FusionReleaseDigests.ComputeSourceSetSha256(invalidSources);

        var exception = Assert.Throws<InvalidDataException>(
            () => FusionReleaseStore.Validate(
                manifest with
                {
                    SourceSetSha256 = sourceSetSha256,
                    Sources = invalidSources,
                    Targets =
                    [
                        manifest.Targets[0] with
                        {
                            SourceSetSha256 = sourceSetSha256
                        }
                    ]
                },
                requireTargets: true));

        Assert.Equal(
            "Fusion source 'products' archive path must remain inside the release.",
            exception.Message);
    }

    [Fact]
    public void GetReleaseTarget_Should_Fail_WhenManifestWasNotUploadedToSelectedApi()
    {
        var deployment = new FusionDeploymentResource(
            "production",
            new NitroResource("nitro")
            {
                CloudUrl = "https://api.chillicream.com",
                ApiId = "other-api"
            });

        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionPipelineExecutor.GetReleaseTarget(
                CreateReleaseManifest(),
                deployment));

        Assert.Equal(
            "Fusion release 'release-1' was not uploaded to Nitro API "
            + "'other-api' at 'https://api.chillicream.com'.",
            exception.Message);
    }

    [Fact]
    public void ValidateManifestSourceNames_Should_Fail_WhenProviderGraphDrifts()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionPipelineExecutor.ValidateManifestSourceNames(
                CreateReleaseManifest(),
                ["reviews"]));

        Assert.Equal(
            "The promoted Fusion source set does not match the AppHost provider "
            + "resources. Missing providers: [products]. Unexpected providers: [reviews].",
            exception.Message);
    }

    [Fact]
    public void ValidateReleaseManifestId_Should_Fail_WhenDraftIsInWrongReleaseDirectory()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => FusionPipelineExecutor.ValidateReleaseManifestId(
                CreateReleaseManifest(),
                "release-2"));

        Assert.Equal(
            "Fusion release manifest ID 'release-1' does not match expected "
            + "release 'release-2'.",
            exception.Message);
    }

    [Fact]
    public void GetTransportEndpoint_Should_Fail_WhenProductionBindingIsMissing()
    {
        using var settings = JsonDocument.Parse(
            """
            {
              "name": "products"
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionPipelineExecutor.GetTransportEndpoint(settings));

        Assert.Equal(
            "Fusion deployment settings must specify an absolute production "
            + "transports.http.url.",
            exception.Message);
    }

    [Fact]
    public void ReplaceDirectoryAtomically_Should_RemoveSource_WhenSourceWasRemoved()
    {
        using var testDirectory = new TestDirectory();
        var destination = Path.Combine(testDirectory.Path, "production");
        var replacement = Path.Combine(testDirectory.Path, "replacement");
        WriteArtifactFile(destination, "sources/products/schema.graphqls");
        WriteArtifactFile(destination, "sources/reviews/schema.graphqls");
        WriteArtifactFile(destination, "nitro-deployment.json");
        WriteArtifactFile(replacement, "sources/products/schema.graphqls");
        WriteArtifactFile(replacement, "nitro-deployment-template.json");

        FusionPipelineExecutor.ReplaceDirectoryAtomically(
            replacement,
            destination);

        GetArtifactFiles(destination)
            .MatchInlineSnapshot(
                """
                nitro-deployment-template.json
                sources/products/schema.graphqls
                """);
    }

    [Fact]
    public void ReplaceDirectoryAtomically_Should_RemoveExtensions_WhenExtensionsWereRemoved()
    {
        using var testDirectory = new TestDirectory();
        var destination = Path.Combine(testDirectory.Path, "production");
        var replacement = Path.Combine(testDirectory.Path, "replacement");
        WriteArtifactFile(destination, "sources/products/schema.graphqls");
        WriteArtifactFile(
            destination,
            "sources/products/schema-extensions.graphqls");
        WriteArtifactFile(replacement, "sources/products/schema.graphqls");

        FusionPipelineExecutor.ReplaceDirectoryAtomically(
            replacement,
            destination);

        GetArtifactFiles(destination)
            .MatchInlineSnapshot("sources/products/schema.graphqls");
    }

    [Theory]
    [InlineData("release/1")]
    [InlineData(@"release\1")]
    [InlineData("release:1")]
    [InlineData("..")]
    public void ValidatePathSegment_Should_Fail_WhenValueIsNotPortable(
        string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionPipelineExecutor.ValidatePathSegment(
                value,
                "configuration tag"));

        Assert.Equal(
            $"Fusion configuration tag '{value}' cannot be used as a portable path segment.",
            exception.Message);
    }

    private static void WriteArtifactFile(
        string root,
        string relativePath)
    {
        var path = Path.Combine(
            root,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "artifact");
    }

    private static string GetArtifactFiles(string root)
        => string.Join(
            Environment.NewLine,
            Directory
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
                .Order());

    private static HashSet<string> GetTransitiveDependencies(
        IReadOnlyDictionary<string, PipelineStep> steps,
        string stepName)
    {
        var dependencies = new HashSet<string>(StringComparer.Ordinal);

        Visit(stepName);
        return dependencies;

        void Visit(string current)
        {
            foreach (var dependency in steps[current].DependsOnSteps)
            {
                if (dependencies.Add(dependency))
                {
                    Visit(dependency);
                }
            }
        }
    }

    private static string GetHttpUrl(JsonDocument settings)
        => settings.RootElement
            .GetProperty("transports")
            .GetProperty("http")
            .GetProperty("url")
            .GetString()!;

    private static FusionReleaseManifest CreateReleaseManifest()
    {
        var compositionSettings =
            FusionReleaseCompositionSettings.From(
                new GraphQLCompositionSettings());
        var source = new FusionReleaseSource(
            "products",
            "release-1",
            "sources/products/release-1.zip",
            new string('a', 64),
            new string('b', 64));
        var sources = new[] { source };
        var sourceSetSha256 =
            FusionReleaseDigests.ComputeSourceSetSha256(sources);

        return new FusionReleaseManifest(
            FusionReleaseManifest.CurrentFormatVersion,
            "release-1",
            FusionReleaseCompatibility.CompositionToolVersion,
            sourceSetSha256,
            new FusionReleaseComposition(
                FusionReleaseDigests.ComputeCompositionSha256(
                    compositionSettings),
                compositionSettings),
            sources,
            [
                new FusionReleaseTarget(
                    "https://api.chillicream.com",
                    "products",
                    sourceSetSha256,
                    [
                        new FusionReleaseSourceReference(
                            source.Name,
                            source.Version,
                            source.ContentSha256)
                    ])
            ]);
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "chilicream-nitro-aspire-tests",
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

#pragma warning restore ASPIREPIPELINES001
